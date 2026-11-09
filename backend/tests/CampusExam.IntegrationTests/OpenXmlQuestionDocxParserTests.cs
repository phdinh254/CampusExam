using CampusExam.Application.QuestionImports;
using CampusExam.Infrastructure.Imports;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using V = DocumentFormat.OpenXml.Vml;
using W = DocumentFormat.OpenXml.Wordprocessing;
using Xunit;

namespace CampusExam.IntegrationTests;

public sealed class OpenXmlQuestionDocxParserTests
{
    [Fact]
    public async Task Preview_parses_question_answers_code_and_media()
    {
        await using var stream = CreateDocument(
            code: "A1-1",
            correctAnswer: "C",
            stemMarker: null,
            note: null,
            includeImage: true);
        var parser = new OpenXmlQuestionDocxParser();

        var preview = await parser.PreviewAsync(stream, CancellationToken.None);

        Assert.Equal(1, preview.TotalRows);
        Assert.Equal(1, preview.ValidRows);
        Assert.Equal(0, preview.ErrorRows);
        Assert.Equal(1, preview.TotalMedia);

        var row = Assert.Single(preview.Rows);
        Assert.Equal("A1-1", row.QuestionCode);
        Assert.Equal("A", row.KqhtSymbol);
        Assert.Equal((byte)1, row.Difficulty);
        Assert.Equal(1, row.SequenceNumber);
        Assert.Contains("{{media:NOIDUNG:1}}", row.QuestionText);
        Assert.Equal("Phương án A", row.AnswerA);
        Assert.Equal("Phương án B", row.AnswerB);
        Assert.Equal("Phương án C", row.AnswerC);
        Assert.Equal("Phương án D", row.AnswerD);
        Assert.Equal("C", row.CorrectAnswer);

        var media = Assert.Single(row.Media);
        Assert.Equal("NOIDUNG", media.Position);
        Assert.Equal((short)1, media.Order);
        Assert.Equal("image/png", media.ContentType);
    }

    [Fact]
    public async Task Read_returns_image_bytes_with_row_position_and_order()
    {
        await using var stream = Imports.DocxCauHoiMau.Tao(
            new Imports.DongMau("A1-1"),
            new Imports.DongMau("A1-2", Hinh: ImagePartType.Png));
        var parser = new OpenXmlQuestionDocxParser();

        var document = await parser.ReadAsync(stream, CancellationToken.None);

        var media = Assert.Single(document.Media);
        Assert.Equal(3, media.RowNumber); // dòng 1 là tiêu đề
        Assert.Equal("NOIDUNG", media.Position);
        Assert.Equal((short)1, media.Order);
        Assert.Equal("image/png", media.ContentType);
        Assert.Equal(Imports.DocxCauHoiMau.OnePixelPng, media.Data);
        Assert.Equal(2, document.Preview.ValidRows);
    }

    [Fact]
    public async Task Non_web_image_format_is_a_warning_not_an_error()
    {
        await using var stream = Imports.DocxCauHoiMau.Tao(new Imports.DongMau("A1-1", Hinh: ImagePartType.Emf));
        var parser = new OpenXmlQuestionDocxParser();

        var preview = await parser.PreviewAsync(stream, CancellationToken.None);

        var row = Assert.Single(preview.Rows);
        Assert.True(row.IsValid);
        var issue = Assert.Single(row.Issues);
        Assert.Equal("UNSUPPORTED_IMAGE_FORMAT", issue.Code);
        Assert.Equal(QuestionImportIssueSeverity.Warning, issue.Severity);
    }

    [Fact]
    public async Task Preview_blocks_missing_code_and_unsupported_stem_marker()
    {
        await using var stream = CreateDocument(
            code: null,
            correctAnswer: "A",
            stemMarker: "X",
            note: "Cần rà soát",
            includeImage: false);
        var parser = new OpenXmlQuestionDocxParser();

        var preview = await parser.PreviewAsync(stream, CancellationToken.None);

        Assert.Equal(0, preview.ValidRows);
        Assert.Equal(1, preview.ErrorRows);
        var row = Assert.Single(preview.Rows);
        Assert.Contains(row.Issues, issue =>
            issue.Code == "MISSING_CODE"
            && issue.Severity == QuestionImportIssueSeverity.Error);
        Assert.Contains(row.Issues, issue =>
            issue.Code == "UNSUPPORTED_STEM_MARKER"
            && issue.Severity == QuestionImportIssueSeverity.Error);
        Assert.Contains(row.Issues, issue =>
            issue.Code == "NOTE_NOT_PERSISTED"
            && issue.Severity == QuestionImportIssueSeverity.Warning);
    }

    [Theory]
    [InlineData("A1-2147483648")]
    [InlineData("A1-99999999999")]
    [InlineData("A1-99999999999999999999999999999")]
    public async Task Preview_reports_sequence_number_overflow_as_row_error(string code)
    {
        await using var stream = CreateDocument(
            code: code,
            correctAnswer: "A",
            stemMarker: null,
            note: null,
            includeImage: false);
        var parser = new OpenXmlQuestionDocxParser();

        var preview = await parser.PreviewAsync(stream, CancellationToken.None);

        Assert.Equal(1, preview.ErrorRows);
        var row = Assert.Single(preview.Rows);
        Assert.Null(row.SequenceNumber);
        Assert.Contains(row.Issues, issue =>
            issue.Code == "INVALID_CODE_FORMAT"
            && issue.Severity == QuestionImportIssueSeverity.Error);
    }

    [Fact]
    public async Task Preview_accepts_largest_supported_sequence_number()
    {
        await using var stream = CreateDocument(
            code: "B3-2147483647",
            correctAnswer: "A",
            stemMarker: null,
            note: null,
            includeImage: false);
        var parser = new OpenXmlQuestionDocxParser();

        var preview = await parser.PreviewAsync(stream, CancellationToken.None);

        var row = Assert.Single(preview.Rows);
        Assert.Equal(int.MaxValue, row.SequenceNumber);
        Assert.DoesNotContain(row.Issues, issue => issue.Code == "INVALID_CODE_FORMAT");
    }

    [Fact]
    public async Task Preview_rejects_document_without_question_table()
    {
        await using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new W.Document(
                new W.Body(new W.Paragraph(new W.Run(new W.Text("Không có bảng")))));
        }

        stream.Position = 0;
        var parser = new OpenXmlQuestionDocxParser();

        var exception = await Assert.ThrowsAsync<QuestionImportFormatException>(
            () => parser.PreviewAsync(stream, CancellationToken.None));

        Assert.Contains("Không tìm thấy bảng câu hỏi", exception.Message);
    }

    [Fact]
    public async Task Preview_rejects_package_exceeding_uncompressed_size_limit()
    {
        await using var stream = CreateDocument("A1-001", "A", null, null, includeImage: false);
        var parser = new OpenXmlQuestionDocxParser(
            GioiHanTepDocx.MacDinh with { TongDungLuongGiaiNen = 1024 });

        var exception = await Assert.ThrowsAsync<QuestionImportFormatException>(
            () => parser.PreviewAsync(stream, CancellationToken.None));

        Assert.Contains("dung lượng sau giải nén", exception.Message);
    }

    [Fact]
    public async Task Preview_rejects_package_with_too_many_entries()
    {
        await using var stream = CreateDocument("A1-001", "A", null, null, includeImage: false);
        var parser = new OpenXmlQuestionDocxParser(GioiHanTepDocx.MacDinh with { SoMucToiDa = 2 });

        var exception = await Assert.ThrowsAsync<QuestionImportFormatException>(
            () => parser.PreviewAsync(stream, CancellationToken.None));

        Assert.Contains("quá nhiều thành phần", exception.Message);
    }

    [Fact]
    public async Task Preview_rejects_xml_part_exceeding_character_limit()
    {
        await using var stream = CreateDocument("A1-001", "A", null, null, includeImage: false);
        var parser = new OpenXmlQuestionDocxParser(GioiHanTepDocx.MacDinh with { KyTuMoiPhan = 200 });

        var exception = await Assert.ThrowsAsync<QuestionImportFormatException>(
            () => parser.PreviewAsync(stream, CancellationToken.None));

        Assert.Contains("quá lớn", exception.Message);
    }

    [Fact]
    public async Task Preview_rejects_non_zip_content()
    {
        await using var stream = new MemoryStream("không phải zip"u8.ToArray());
        var parser = new OpenXmlQuestionDocxParser();

        await Assert.ThrowsAsync<QuestionImportFormatException>(
            () => parser.PreviewAsync(stream, CancellationToken.None));
    }

    private static MemoryStream CreateDocument(
        string? code,
        string correctAnswer,
        string? stemMarker,
        string? note,
        bool includeImage)
    {
        var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(
                   stream,
                   WordprocessingDocumentType.Document,
                   autoSave: true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new W.Document(new W.Body());
            var body = mainPart.Document.Body!;
            var table = new W.Table();

            table.Append(CreateRow(
                CreateTextCell("TT"),
                CreateTextCell("Mã câu hỏi"),
                CreateTextCell("Nội dung câu hỏi"),
                CreateTextCell("Đáp án đúng"),
                CreateTextCell("Đánh dấu câu dẫn"),
                CreateTextCell("Ghi chú")));

            var contentCell = new W.TableCell();
            var questionParagraph = new W.Paragraph(
                new W.Run(new W.Text("Nội dung có hình ")));

            if (includeImage)
            {
                var imagePart = mainPart.AddImagePart(ImagePartType.Png);
                using var imageStream = new MemoryStream(OnePixelPng);
                imagePart.FeedData(imageStream);
                var relationshipId = mainPart.GetIdOfPart(imagePart);
                var shape = new V.Shape
                {
                    Id = "question-image",
                    Style = "width:10pt;height:10pt"
                };
                shape.Append(new V.ImageData { RelationshipId = relationshipId });
                questionParagraph.Append(new W.Run(new W.Picture(shape)));
            }

            questionParagraph.Append(new W.Run(new W.Text(" ?")));
            contentCell.Append(
                questionParagraph,
                new W.Paragraph(new W.Run(new W.Text("A. Phương án A"))),
                new W.Paragraph(new W.Run(new W.Text("B. Phương án B"))),
                new W.Paragraph(new W.Run(new W.Text("C. Phương án C"))),
                new W.Paragraph(new W.Run(new W.Text("D. Phương án D"))));

            table.Append(CreateRow(
                CreateTextCell("1"),
                CreateTextCell(code),
                contentCell,
                CreateTextCell(correctAnswer),
                CreateTextCell(stemMarker),
                CreateTextCell(note)));

            body.Append(table);
        }

        stream.Position = 0;
        return stream;
    }

    private static W.TableRow CreateRow(params W.TableCell[] cells) =>
        new(cells);

    private static W.TableCell CreateTextCell(string? value) =>
        new(new W.Paragraph(new W.Run(new W.Text(value ?? string.Empty))));

    private static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");
}
