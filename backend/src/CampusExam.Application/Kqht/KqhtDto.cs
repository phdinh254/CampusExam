using CampusExam.Application.Common.Models;

namespace CampusExam.Application.Kqht;

/// <param name="SoCauHoi">Số câu hỏi thuộc KQHT; lớn hơn 0 thì không đổi được thứ tự.</param>
public sealed record KqhtDto(
    int MaKQHT,
    string TenKQHT,
    byte ThuTu,
    string? MoTa,
    TrangThaiSuDung TrangThai,
    string KyHieu,
    int SoCauHoi = 0);
