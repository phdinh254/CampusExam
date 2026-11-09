using System.Globalization;
using System.IO.Compression;
using System.IO.Packaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using CampusExam.Application.QuestionImports;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using A = DocumentFormat.OpenXml.Drawing;
using V = DocumentFormat.OpenXml.Vml;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace CampusExam.Infrastructure.Imports;

public sealed partial class OpenXmlQuestionDocxParser(GioiHanTepDocx gioiHan) : IQuestionDocxParser
{
    private const string ImagePlaceholderPrefix = "{{image:";

    public OpenXmlQuestionDocxParser()
        : this(GioiHanTepDocx.MacDinh)
    {
    }

    /// <summary>Định dạng hình trình duyệt hiển thị được; định dạng khác (EMF, WMF, TIFF...) chỉ cảnh báo.</summary>
    private static readonly HashSet<string> DinhDangHinhWeb = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "image/gif", "image/webp", "image/bmp"
    };

    public Task<QuestionImportPreview> PreviewAsync(
        Stream docxStream,
        CancellationToken cancellationToken) =>
        Task.FromResult(Doc(docxStream, mediaSink: null, cancellationToken));

    public Task<QuestionImportDocument> ReadAsync(
        Stream docxStream,
        CancellationToken cancellationToken)
    {
        var media = new List<QuestionImportMediaContent>();
        var preview = Doc(docxStream, media, cancellationToken);
        return Task.FromResult(new QuestionImportDocument(preview, media));
    }

    private QuestionImportPreview Doc(
        Stream docxStream,
        List<QuestionImportMediaContent>? mediaSink,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(docxStream);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            KiemTraKichThuocGoi(docxStream);

            using var document = WordprocessingDocument.Open(
                docxStream,
                isEditable: false,
                new OpenSettings { AutoSave = false, MaxCharactersInPart = gioiHan.KyTuMoiPhan });

            return Parse(document, mediaSink, cancellationToken);
        }
        catch (Exception exception) when (LaLoiVuotKyTu(exception))
        {
            throw new QuestionImportFormatException(
                "Tài liệu DOCX có phần nội dung quá lớn, không thể đọc.",
                exception);
        }
        catch (Exception exception) when (
            exception is OpenXmlPackageException or
            FileFormatException or
            InvalidDataException or
            IOException)
        {
            throw new QuestionImportFormatException(
                "Tệp không phải tài liệu DOCX hợp lệ hoặc đã bị hỏng.",
                exception);
        }
    }

    /// <summary>
    /// Đọc thư mục trung tâm của zip (không giải nén) để chặn zip bomb trước khi Open XML đọc
    /// nội dung. .NET không giải nén một mục vượt dung lượng khai báo, nên tổng khai báo là đáng tin.
    /// </summary>
    private void KiemTraKichThuocGoi(Stream stream)
    {
        if (!stream.CanSeek)
        {
            throw new ArgumentException("Luồng DOCX phải hỗ trợ seek.", nameof(stream));
        }

        var viTriBatDau = stream.Position;
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
        {
            if (zip.Entries.Count > gioiHan.SoMucToiDa)
            {
                throw new QuestionImportFormatException(
                    "Tài liệu DOCX chứa quá nhiều thành phần, không thể đọc.");
            }

            long tong = 0;
            foreach (var entry in zip.Entries)
            {
                tong += entry.Length;
                if (tong > gioiHan.TongDungLuongGiaiNen)
                {
                    throw new QuestionImportFormatException(
                        $"Tài liệu DOCX có dung lượng sau giải nén vượt quá " +
                        $"{gioiHan.TongDungLuongGiaiNen / (1024 * 1024)} MB, không thể đọc.");
                }
            }
        }

        stream.Position = viTriBatDau;
    }

    /// <summary>Open XML báo vượt MaxCharactersInPart bằng XmlException hoặc bọc trong exception khác.</summary>
    private static bool LaLoiVuotKyTu(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is XmlException)
            {
                return true;
            }
        }

        return false;
    }

    private static QuestionImportPreview Parse(
        WordprocessingDocument document,
        List<QuestionImportMediaContent>? mediaSink,
        CancellationToken cancellationToken)
    {
        var mainPart = document.MainDocumentPart
            ?? throw new QuestionImportFormatException(
                "Tài liệu DOCX không có nội dung chính.");
        var body = mainPart.Document?.Body
            ?? throw new QuestionImportFormatException(
                "Tài liệu DOCX không có phần thân.");
        var table = body
            .Descendants<W.Table>()
            .FirstOrDefault(IsQuestionTable)
            ?? throw new QuestionImportFormatException(
                "Không tìm thấy bảng câu hỏi gồm các cột Mã câu hỏi, Nội dung câu hỏi và Đáp án đúng.");

        var tableRows = table.Elements<W.TableRow>().ToList();
        var rows = new List<QuestionImportRowPreview>();
        var seenCodes = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 1; index < tableRows.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var cells = tableRows[index].Elements<W.TableCell>().ToList();
            if (cells.Count < 6 || cells.All(cell => string.IsNullOrWhiteSpace(GetPlainText(cell))))
            {
                continue;
            }

            var row = ParseRow(mainPart, cells, index + 1, seenCodes, mediaSink);
            rows.Add(row);
        }

        return new QuestionImportPreview(
            rows.Count,
            rows.Count(row => row.IsValid),
            rows.Count(row => !row.IsValid),
            rows.Sum(row => row.Media.Count),
            rows);
    }

    private static QuestionImportRowPreview ParseRow(
        MainDocumentPart mainPart,
        IReadOnlyList<W.TableCell> cells,
        int rowNumber,
        ISet<string> seenCodes,
        List<QuestionImportMediaContent>? mediaSink)
    {
        var issues = new List<QuestionImportIssue>();
        var code = NullIfWhiteSpace(GetPlainText(cells[1]));
        string? kqhtSymbol = null;
        byte? difficulty = null;
        int? sequenceNumber = null;

        if (code is null)
        {
            AddError(issues, "MISSING_CODE", "Mã câu hỏi là bắt buộc.");
        }
        else
        {
            var codeMatch = QuestionCodeRegex().Match(code);
            if (!codeMatch.Success)
            {
                AddError(
                    issues,
                    "INVALID_CODE_FORMAT",
                    "Mã câu hỏi phải có dạng [A-Z][1-3]-[số nguyên dương], ví dụ A1-1.");
            }
            else if (!int.TryParse(
                         codeMatch.Groups["sequence"].Value,
                         NumberStyles.None,
                         CultureInfo.InvariantCulture,
                         out var parsedSequence))
            {
                // CAUHOI.STT là int: số thứ tự vượt 2147483647 không lưu được.
                AddError(
                    issues,
                    "INVALID_CODE_FORMAT",
                    $"Số thứ tự trong mã câu hỏi không được vượt quá {int.MaxValue}.");
            }
            else
            {
                kqhtSymbol = codeMatch.Groups["kqht"].Value;
                difficulty = byte.Parse(
                    codeMatch.Groups["difficulty"].Value,
                    CultureInfo.InvariantCulture);
                sequenceNumber = parsedSequence;

                if (!seenCodes.Add(code))
                {
                    AddError(
                        issues,
                        "DUPLICATE_CODE_IN_FILE",
                        $"Mã câu hỏi {code} bị lặp trong tệp import.");
                }
            }
        }

        var richContent = ReadRichContent(mainPart, cells[2], issues);
        var sections = SplitQuestionAndAnswers(richContent.Text, issues);
        var media = new List<QuestionImportMediaPreview>();

        var questionText = MaterializeSection(
            sections.Question,
            "NOIDUNG",
            richContent.Media,
            media);
        var answerA = MaterializeSection(
            sections.AnswerA,
            "DAA",
            richContent.Media,
            media);
        var answerB = MaterializeSection(
            sections.AnswerB,
            "DAB",
            richContent.Media,
            media);
        var answerC = MaterializeSection(
            sections.AnswerC,
            "DAC",
            richContent.Media,
            media);
        var answerD = MaterializeSection(
            sections.AnswerD,
            "DAD",
            richContent.Media,
            media);

        ValidateRequiredContent(questionText, "EMPTY_QUESTION", "Nội dung câu hỏi bị trống.", issues);
        ValidateRequiredContent(answerA, "EMPTY_ANSWER_A", "Phương án A bị trống.", issues);
        ValidateRequiredContent(answerB, "EMPTY_ANSWER_B", "Phương án B bị trống.", issues);
        ValidateRequiredContent(answerC, "EMPTY_ANSWER_C", "Phương án C bị trống.", issues);
        ValidateRequiredContent(answerD, "EMPTY_ANSWER_D", "Phương án D bị trống.", issues);

        var correctAnswer = NullIfWhiteSpace(GetPlainText(cells[3]))?.ToUpperInvariant();
        if (correctAnswer is null || !CorrectAnswerRegex().IsMatch(correctAnswer))
        {
            AddError(
                issues,
                "INVALID_CORRECT_ANSWER",
                "Đáp án đúng phải là một trong A, B, C hoặc D.");
        }

        var stemMarker = NullIfWhiteSpace(GetPlainText(cells[4]));
        if (stemMarker is not null)
        {
            AddError(
                issues,
                "UNSUPPORTED_STEM_MARKER",
                "Đánh dấu câu dẫn chưa được hỗ trợ vì chưa có quy tắc nghiệp vụ được xác nhận.");
        }

        var note = NullIfWhiteSpace(GetPlainText(cells[5]));
        if (note is not null)
        {
            issues.Add(new QuestionImportIssue(
                "NOTE_NOT_PERSISTED",
                "Ghi chú chỉ hiển thị ở preview và chưa được lưu lâu dài.",
                QuestionImportIssueSeverity.Warning));
        }

        var referencedImageIds = media
            .Select(item => item.Marker)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var source in richContent.Media.Values)
        {
            if (!referencedImageIds.Contains(source.FinalMarker))
            {
                AddError(
                    issues,
                    "UNPLACED_MEDIA",
                    $"Không xác định được vị trí của hình {source.FileName}.");
                continue;
            }

            if (!DinhDangHinhWeb.Contains(source.ContentType))
            {
                issues.Add(new QuestionImportIssue(
                    "UNSUPPORTED_IMAGE_FORMAT",
                    $"Hình {source.FileName} ({source.ContentType}) vẫn được lưu nhưng trình duyệt có thể không hiển thị; nên chuyển sang PNG/JPEG.",
                    QuestionImportIssueSeverity.Warning));
            }

            if (mediaSink is not null)
            {
                using var data = new MemoryStream();
                using (var partStream = source.Part.GetStream(FileMode.Open, FileAccess.Read))
                {
                    partStream.CopyTo(data);
                }

                mediaSink.Add(new QuestionImportMediaContent(
                    rowNumber,
                    source.Position,
                    source.Order,
                    source.FileName,
                    source.ContentType,
                    data.ToArray()));
            }
        }

        return new QuestionImportRowPreview(
            rowNumber,
            code,
            kqhtSymbol,
            difficulty,
            sequenceNumber,
            questionText,
            answerA,
            answerB,
            answerC,
            answerD,
            correctAnswer,
            stemMarker,
            note,
            media,
            issues);
    }

    private static bool IsQuestionTable(W.Table table)
    {
        var headerCells = table
            .Elements<W.TableRow>()
            .FirstOrDefault()?
            .Elements<W.TableCell>()
            .Select(cell => NormalizeHeader(GetPlainText(cell)))
            .ToList();

        return headerCells is { Count: >= 6 }
            && headerCells.Any(value => value.Contains("MACAUHOI", StringComparison.Ordinal))
            && headerCells.Any(value => value.Contains("NOIDUNGCAUHOI", StringComparison.Ordinal))
            && headerCells.Any(value => value.Contains("DAPANDUNG", StringComparison.Ordinal));
    }

    private static RichCellContent ReadRichContent(
        MainDocumentPart mainPart,
        W.TableCell cell,
        ICollection<QuestionImportIssue> issues)
    {
        var builder = new StringBuilder();
        var media = new Dictionary<int, MediaSource>();
        var imageNumber = 0;

        foreach (var paragraph in cell.Descendants<W.Paragraph>())
        {
            foreach (var child in paragraph.ChildElements)
            {
                AppendElement(mainPart, child, builder, media, issues, ref imageNumber);
            }

            builder.AppendLine();
        }

        return new RichCellContent(builder.ToString(), media);
    }

    private static void AppendElement(
        MainDocumentPart mainPart,
        OpenXmlElement element,
        StringBuilder builder,
        IDictionary<int, MediaSource> media,
        ICollection<QuestionImportIssue> issues,
        ref int imageNumber)
    {
        if (element is W.Drawing or W.Picture)
        {
            imageNumber++;
            var placeholder = $"{ImagePlaceholderPrefix}{imageNumber}}}}}";
            builder.Append(placeholder);

            var relationshipId = GetImageRelationshipId(element);
            if (relationshipId is null || mainPart.GetPartById(relationshipId) is not ImagePart imagePart)
            {
                AddError(
                    issues,
                    "BROKEN_MEDIA_RELATIONSHIP",
                    $"Không đọc được dữ liệu hình thứ {imageNumber} trong dòng.");
                return;
            }

            media[imageNumber] = new MediaSource(
                imageNumber,
                Path.GetFileName(imagePart.Uri.OriginalString),
                imagePart.ContentType,
                imagePart);
            return;
        }

        switch (element)
        {
            case W.Text text:
                builder.Append(text.Text);
                return;
            case W.TabChar:
                builder.Append('\t');
                return;
            case W.Break:
            case W.CarriageReturn:
                builder.AppendLine();
                return;
        }

        foreach (var child in element.ChildElements)
        {
            AppendElement(mainPart, child, builder, media, issues, ref imageNumber);
        }
    }

    private static string? GetImageRelationshipId(OpenXmlElement element)
    {
        var drawingRelationship = element
            .Descendants<A.Blip>()
            .Select(blip => blip.Embed?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (drawingRelationship is not null)
        {
            return drawingRelationship;
        }

        return element
            .Descendants<V.ImageData>()
            .Select(image => image.RelationshipId?.Value)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static ParsedSections SplitQuestionAndAnswers(
        string content,
        ICollection<QuestionImportIssue> issues)
    {
        var matches = AnswerLabelRegex().Matches(content);
        if (matches.Count < 4)
        {
            AddError(
                issues,
                "INVALID_ANSWER_STRUCTURE",
                "Không tách được đủ bốn phương án A, B, C và D từ ô nội dung.");
            return new ParsedSections(content, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        var selected = new Match[4];
        var expectedLabels = new[] { "A", "B", "C", "D" };
        var searchIndex = 0;
        foreach (var match in matches.Cast<Match>())
        {
            if (searchIndex < expectedLabels.Length
                && match.Groups["label"].Value.Equals(
                    expectedLabels[searchIndex],
                    StringComparison.OrdinalIgnoreCase))
            {
                selected[searchIndex] = match;
                searchIndex++;
            }
        }

        if (searchIndex != expectedLabels.Length)
        {
            AddError(
                issues,
                "INVALID_ANSWER_STRUCTURE",
                "Các phương án phải xuất hiện theo thứ tự A, B, C và D.");
            return new ParsedSections(content, string.Empty, string.Empty, string.Empty, string.Empty);
        }

        return new ParsedSections(
            content[..selected[0].Index],
            Slice(content, selected[0].Index + selected[0].Length, selected[1].Index),
            Slice(content, selected[1].Index + selected[1].Length, selected[2].Index),
            Slice(content, selected[2].Index + selected[2].Length, selected[3].Index),
            content[(selected[3].Index + selected[3].Length)..]);
    }

    private static string MaterializeSection(
        string section,
        string position,
        IReadOnlyDictionary<int, MediaSource> mediaSources,
        ICollection<QuestionImportMediaPreview> outputMedia)
    {
        short order = 0;
        var replaced = ImagePlaceholderRegex().Replace(section, match =>
        {
            var id = int.Parse(match.Groups["id"].Value, CultureInfo.InvariantCulture);
            if (!mediaSources.TryGetValue(id, out var source))
            {
                return string.Empty;
            }

            order++;
            var marker = $"{{{{media:{position}:{order}}}}}";
            source.FinalMarker = marker;
            source.Position = position;
            source.Order = order;
            outputMedia.Add(new QuestionImportMediaPreview(
                marker,
                position,
                order,
                source.FileName,
                source.ContentType));
            return marker;
        });

        return NormalizeContent(replaced);
    }

    private static string Slice(string value, int start, int end) =>
        value[start..end];

    private static string GetPlainText(OpenXmlElement element) =>
        NormalizeContent(string.Join(
            "\n",
            element.Descendants<W.Paragraph>().Select(paragraph => paragraph.InnerText)));

    private static string NormalizeContent(string value)
    {
        var normalized = value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        normalized = ExtraBlankLinesRegex().Replace(normalized, "\n\n");
        return normalized.Trim();
    }

    private static string NormalizeHeader(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (character is 'Đ' or 'đ')
            {
                builder.Append('D');
                continue;
            }

            if (CharUnicodeInfo.GetUnicodeCategory(character)
                != UnicodeCategory.NonSpacingMark
                && char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToUpperInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static string? NullIfWhiteSpace(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateRequiredContent(
        string value,
        string code,
        string message,
        ICollection<QuestionImportIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            AddError(issues, code, message);
        }
    }

    private static void AddError(
        ICollection<QuestionImportIssue> issues,
        string code,
        string message) =>
        issues.Add(new QuestionImportIssue(
            code,
            message,
            QuestionImportIssueSeverity.Error));

    private sealed record RichCellContent(
        string Text,
        IReadOnlyDictionary<int, MediaSource> Media);

    private sealed record ParsedSections(
        string Question,
        string AnswerA,
        string AnswerB,
        string AnswerC,
        string AnswerD);

    private sealed class MediaSource(
        int id,
        string fileName,
        string contentType,
        ImagePart part)
    {
        public int Id { get; } = id;

        public string FileName { get; } = fileName;

        public string ContentType { get; } = contentType;

        public ImagePart Part { get; } = part;

        public string FinalMarker { get; set; } = string.Empty;

        public string Position { get; set; } = string.Empty;

        public short Order { get; set; }
    }

    [GeneratedRegex(
        "^(?<kqht>[A-Z])(?<difficulty>[1-3])-(?<sequence>[1-9][0-9]*)$",
        RegexOptions.CultureInvariant)]
    private static partial Regex QuestionCodeRegex();

    [GeneratedRegex("^[A-D]$", RegexOptions.CultureInvariant)]
    private static partial Regex CorrectAnswerRegex();

    [GeneratedRegex(
        @"(?m)^\s*(?<label>[A-D])\s*[\.\):]\s*",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex AnswerLabelRegex();

    [GeneratedRegex(
        @"\{\{image:(?<id>[0-9]+)\}\}",
        RegexOptions.CultureInvariant)]
    private static partial Regex ImagePlaceholderRegex();

    [GeneratedRegex(@"\n{3,}", RegexOptions.CultureInvariant)]
    private static partial Regex ExtraBlankLinesRegex();
}
