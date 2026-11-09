namespace CampusExam.Domain.Entities;

/// <summary>
/// Phân công thí sinh vào kỳ thi (PHANCONGTHI). Hiện chỉ ánh xạ cột cần để đếm và chặn xóa
/// thí sinh; các cột còn lại ánh xạ khi làm phần kỳ thi.
/// </summary>
public class PhanCongThi
{
    public int MaPhanCong { get; set; }

    public int MaKT { get; set; }

    public string Msts { get; set; } = string.Empty;
}

/// <summary>Bài làm (BAILAM). Hiện chỉ ánh xạ khóa để đếm số lượt thi.</summary>
public class BaiLam
{
    public int MaBL { get; set; }

    public int MaPhanCong { get; set; }
}
