using System.Text.Json;
using CampusExam.Application.Common.Models;
using CampusExam.Application.Kqht;
using CampusExam.Application.QuestionImports;
using Xunit;

namespace CampusExam.UnitTests;

/// <summary>
/// Khóa tên trường JSON theo docs/API_DESIGN.md: frontend phụ thuộc trực tiếp vào các tên này.
/// </summary>
public sealed class ApiContractJsonTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData(TrangThaiSuDung.DangSuDung, "dang_su_dung")]
    [InlineData(TrangThaiSuDung.NgungSuDung, "ngung_su_dung")]
    public void TrangThaiSuDung_serializes_as_vietnamese_string(TrangThaiSuDung value, string expected)
    {
        Assert.Equal($"\"{expected}\"", JsonSerializer.Serialize(value, WebOptions));
        Assert.Equal(value, JsonSerializer.Deserialize<TrangThaiSuDung>($"\"{expected}\"", WebOptions));
    }

    [Theory]
    [InlineData("\"active\"")]
    [InlineData("\"DangSuDung\"")]
    [InlineData("1")]
    public void TrangThaiSuDung_rejects_unknown_values_and_numbers(string json)
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<TrangThaiSuDung>(json, WebOptions));
    }

    [Fact]
    public void TrangDuLieu_uses_vietnamese_field_names_and_computes_page_count()
    {
        var trang = new TrangDuLieu<int>([1, 2], Trang: 2, KichThuocTrang: 20, TongSoDong: 41);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(trang, WebOptions));
        var root = document.RootElement;

        Assert.Equal(2, root.GetProperty("duLieu").GetArrayLength());
        Assert.Equal(2, root.GetProperty("trang").GetInt32());
        Assert.Equal(20, root.GetProperty("kichThuocTrang").GetInt32());
        Assert.Equal(41, root.GetProperty("tongSoDong").GetInt32());
        Assert.Equal(3, root.GetProperty("tongSoTrang").GetInt32());
    }

    [Fact]
    public void TrangDuLieu_with_no_rows_has_zero_pages()
    {
        Assert.Equal(0, new TrangDuLieu<int>([], 1, 20, 0).TongSoTrang);
    }

    [Fact]
    public void QuestionImportPreview_uses_vietnamese_field_names()
    {
        var preview = new QuestionImportPreview(
            TotalRows: 1,
            ValidRows: 0,
            ErrorRows: 1,
            TotalMedia: 1,
            Rows:
            [
                new QuestionImportRowPreview(
                    RowNumber: 2,
                    QuestionCode: "A1-1",
                    KqhtSymbol: "A",
                    Difficulty: 1,
                    SequenceNumber: 1,
                    QuestionText: "Câu hỏi",
                    AnswerA: "a",
                    AnswerB: "b",
                    AnswerC: "c",
                    AnswerD: "d",
                    CorrectAnswer: "B",
                    StemMarker: null,
                    Note: "Ghi chú",
                    Media: [new QuestionImportMediaPreview("[[IMG1]]", "NOIDUNG", 1, "a.png", "image/png")],
                    Issues:
                    [
                        new QuestionImportIssue("DUPLICATE_CODE", "Trùng mã", QuestionImportIssueSeverity.Error),
                        new QuestionImportIssue("NOTE_IGNORED", "Bỏ ghi chú", QuestionImportIssueSeverity.Warning)
                    ])
            ]);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(preview, WebOptions));
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("tongDong").GetInt32());
        Assert.Equal(0, root.GetProperty("soDongHopLe").GetInt32());
        Assert.Equal(1, root.GetProperty("soDongLoi").GetInt32());
        Assert.Equal(1, root.GetProperty("tongHinhAnh").GetInt32());

        var dong = root.GetProperty("dong")[0];
        Assert.Equal(2, dong.GetProperty("soDong").GetInt32());
        Assert.Equal("A1-1", dong.GetProperty("maCauHoi").GetString());
        Assert.Equal("A", dong.GetProperty("kyHieu").GetString());
        Assert.Equal(1, dong.GetProperty("maMD").GetByte());
        Assert.Equal(1, dong.GetProperty("stt").GetInt32());
        Assert.Equal("Câu hỏi", dong.GetProperty("noiDung").GetString());
        Assert.Equal("a", dong.GetProperty("phuongAnA").GetString());
        Assert.Equal("b", dong.GetProperty("phuongAnB").GetString());
        Assert.Equal("c", dong.GetProperty("phuongAnC").GetString());
        Assert.Equal("d", dong.GetProperty("phuongAnD").GetString());
        Assert.Equal("B", dong.GetProperty("dapAnDung").GetString());
        Assert.Equal(JsonValueKind.Null, dong.GetProperty("danhDauCauDan").ValueKind);
        Assert.Equal("Ghi chú", dong.GetProperty("ghiChu").GetString());
        Assert.False(dong.GetProperty("hopLe").GetBoolean());

        var hinh = dong.GetProperty("hinhAnh")[0];
        Assert.Equal("[[IMG1]]", hinh.GetProperty("maDanhDau").GetString());
        Assert.Equal("NOIDUNG", hinh.GetProperty("viTri").GetString());
        Assert.Equal(1, hinh.GetProperty("thuTu").GetInt16());
        Assert.Equal("a.png", hinh.GetProperty("tenFile").GetString());
        Assert.Equal("image/png", hinh.GetProperty("mimeType").GetString());

        var vanDe = dong.GetProperty("vanDe");
        Assert.Equal("DUPLICATE_CODE", vanDe[0].GetProperty("code").GetString());
        Assert.Equal("Trùng mã", vanDe[0].GetProperty("noiDung").GetString());
        Assert.Equal("loi", vanDe[0].GetProperty("mucDo").GetString());
        Assert.Equal("canh_bao", vanDe[1].GetProperty("mucDo").GetString());

        Assert.False(root.TryGetProperty("totalRows", out _));
        Assert.False(dong.TryGetProperty("isValid", out _));
    }
}
