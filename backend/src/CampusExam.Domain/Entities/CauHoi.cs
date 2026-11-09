namespace CampusExam.Domain.Entities;

public class CauHoi
{
    public const short TrangThaiNgungSuDung = 0;
    public const short TrangThaiDangSuDung = 1;

    public int MaCH { get; set; }

    public int? MaLanImport { get; set; }

    public byte MaMD { get; set; }

    public int MaKQHT { get; set; }

    public int Stt { get; set; }

    public string NoiDung { get; set; } = string.Empty;

    public string DaA { get; set; } = string.Empty;

    public string DaB { get; set; } = string.Empty;

    public string DaC { get; set; } = string.Empty;

    public string DaD { get; set; } = string.Empty;

    public string DapAnDung { get; set; } = string.Empty;

    public short CoDinhDa { get; set; }

    public short TrangThai { get; set; }

    public DateTime NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public string MaCauHoi { get; set; } = string.Empty;

    public string KyHieu { get; set; } = string.Empty;

    public byte[] PhienBan { get; set; } = [];

    /// <summary>Mã câu hỏi chuẩn <c>[KyHieu][MaMD]-[STT]</c>, ví dụ <c>A1-1</c>.</summary>
    public static string TaoMaCauHoi(string kyHieu, byte maMD, int stt) => $"{kyHieu}{maMD}-{stt}";
}
