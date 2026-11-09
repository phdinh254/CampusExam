using System.Text.Json.Serialization;
using CampusExam.Application.Common.Json;

namespace CampusExam.Application.QuestionImports;

// Tên thuộc tính C# giữ tiếng Anh cho parser; tên trường JSON là hợp đồng API
// tiếng Việt theo docs/API_DESIGN.md mục 2.5.

[JsonConverter(typeof(EnumChuoiConverter<QuestionImportIssueSeverity>))]
public enum QuestionImportIssueSeverity
{
    [JsonStringEnumMemberName("canh_bao")]
    Warning,

    [JsonStringEnumMemberName("loi")]
    Error
}

public sealed record QuestionImportIssue(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("noiDung")] string Message,
    [property: JsonPropertyName("mucDo")] QuestionImportIssueSeverity Severity);

public sealed record QuestionImportMediaPreview(
    [property: JsonPropertyName("maDanhDau")] string Marker,
    [property: JsonPropertyName("viTri")] string Position,
    [property: JsonPropertyName("thuTu")] short Order,
    [property: JsonPropertyName("tenFile")] string FileName,
    [property: JsonPropertyName("mimeType")] string ContentType);

public sealed record QuestionImportRowPreview(
    [property: JsonPropertyName("soDong")] int RowNumber,
    [property: JsonPropertyName("maCauHoi")] string? QuestionCode,
    [property: JsonPropertyName("kyHieu")] string? KqhtSymbol,
    [property: JsonPropertyName("maMD")] byte? Difficulty,
    [property: JsonPropertyName("stt")] int? SequenceNumber,
    [property: JsonPropertyName("noiDung")] string QuestionText,
    [property: JsonPropertyName("phuongAnA")] string AnswerA,
    [property: JsonPropertyName("phuongAnB")] string AnswerB,
    [property: JsonPropertyName("phuongAnC")] string AnswerC,
    [property: JsonPropertyName("phuongAnD")] string AnswerD,
    [property: JsonPropertyName("dapAnDung")] string? CorrectAnswer,
    [property: JsonPropertyName("danhDauCauDan")] string? StemMarker,
    [property: JsonPropertyName("ghiChu")] string? Note,
    [property: JsonPropertyName("hinhAnh")] IReadOnlyList<QuestionImportMediaPreview> Media,
    [property: JsonPropertyName("vanDe")] IReadOnlyList<QuestionImportIssue> Issues)
{
    [JsonPropertyName("hopLe")]
    public bool IsValid => Issues.All(issue =>
        issue.Severity != QuestionImportIssueSeverity.Error);
}

public sealed record QuestionImportPreview(
    [property: JsonPropertyName("tongDong")] int TotalRows,
    [property: JsonPropertyName("soDongHopLe")] int ValidRows,
    [property: JsonPropertyName("soDongLoi")] int ErrorRows,
    [property: JsonPropertyName("tongHinhAnh")] int TotalMedia,
    [property: JsonPropertyName("dong")] IReadOnlyList<QuestionImportRowPreview> Rows)
{
    /// <summary>Mã bản xem trước để xác nhận nhập; null khi còn dòng lỗi (không xác nhận được).</summary>
    [JsonPropertyName("maXemTruoc")]
    public string? PreviewId { get; init; }

    /// <summary>Hạn xác nhận (UTC); null khi <see cref="PreviewId"/> null.</summary>
    [JsonPropertyName("hetHan")]
    public DateTime? ExpiresAt { get; init; }

    /// <summary>Tạo lại bản xem trước với danh sách dòng mới và đếm lại số dòng hợp lệ/lỗi.</summary>
    public static QuestionImportPreview FromRows(IReadOnlyList<QuestionImportRowPreview> rows) => new(
        rows.Count,
        rows.Count(row => row.IsValid),
        rows.Count(row => !row.IsValid),
        rows.Sum(row => row.Media.Count),
        rows);
}

public sealed class QuestionImportFormatException(
    string message,
    Exception? innerException = null) : Exception(message, innerException);
