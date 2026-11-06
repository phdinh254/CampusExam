using CampusExam.Application.Common.Models;

namespace CampusExam.Application.Kqht;

public sealed record KqhtDto(
    int MaKQHT,
    string TenKQHT,
    byte ThuTu,
    string? MoTa,
    TrangThaiSuDung TrangThai,
    string KyHieu);
