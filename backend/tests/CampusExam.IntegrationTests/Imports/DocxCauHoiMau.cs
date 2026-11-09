using DocumentFormat.OpenXml.Packaging;
using V = DocumentFormat.OpenXml.Vml;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace CampusExam.IntegrationTests.Imports;

/// <summary>Một dòng của bảng IU03 trong tệp mẫu.</summary>
/// <param name="Hinh">Kiểu hình chèn vào nội dung câu hỏi; null là không có hình.</param>
public sealed record DongMau(
    string? Ma,
    string NoiDung = "Câu hỏi mẫu",
    string DapAn = "A",
    PartTypeInfo? Hinh = null,
    string? PhuongAnA = null);

/// <summary>Sinh tệp DOCX theo mẫu bảng IU03 (6 cột) để kiểm thử nhập câu hỏi.</summary>
public static class DocxCauHoiMau
{
    public static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=");

    public static MemoryStream Tao(params DongMau[] dong)
    {
        var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, DocumentFormat.OpenXml.WordprocessingDocumentType.Document, autoSave: true))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new W.Document(new W.Body());
            var table = new W.Table(Hang(
                O("TT"), O("Mã câu hỏi"), O("Nội dung câu hỏi"), O("Đáp án đúng"), O("Đánh dấu câu dẫn"), O("Ghi chú")));

            for (var i = 0; i < dong.Length; i++)
            {
                var d = dong[i];
                var cauHoi = new W.Paragraph(new W.Run(new W.Text(d.NoiDung) { Space = DocumentFormat.OpenXml.SpaceProcessingModeValues.Preserve }));
                if (d.Hinh is { } kieu)
                {
                    var imagePart = mainPart.AddImagePart(kieu);
                    using (var data = new MemoryStream(OnePixelPng))
                    {
                        imagePart.FeedData(data);
                    }

                    var shape = new V.Shape { Id = $"hinh-{i}", Style = "width:10pt;height:10pt" };
                    shape.Append(new V.ImageData { RelationshipId = mainPart.GetIdOfPart(imagePart) });
                    cauHoi.Append(new W.Run(new W.Picture(shape)));
                }

                var noiDung = new W.TableCell(
                    cauHoi,
                    DoanVan($"A. {d.PhuongAnA ?? "Phương án A"}"),
                    DoanVan("B. Phương án B"),
                    DoanVan("C. Phương án C"),
                    DoanVan("D. Phương án D"));

                table.Append(Hang(O((i + 1).ToString()), O(d.Ma), noiDung, O(d.DapAn), O(null), O(null)));
            }

            mainPart.Document.Body!.Append(table);
        }

        stream.Position = 0;
        return stream;
    }

    private static W.TableRow Hang(params W.TableCell[] cells) => new(cells);

    private static W.TableCell O(string? value) => new(DoanVan(value ?? string.Empty));

    private static W.Paragraph DoanVan(string text) => new(new W.Run(new W.Text(text)));
}
