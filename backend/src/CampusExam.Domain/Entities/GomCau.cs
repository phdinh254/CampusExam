namespace CampusExam.Domain.Entities;

/// <summary>
/// Câu hỏi nằm trong đề gốc (bảng GOM_CAU). Hiện chỉ ánh xạ khóa để đếm số đề đã dùng
/// một câu hỏi; các cột bản sao nội dung sẽ ánh xạ khi làm phần sinh đề.
/// </summary>
public class GomCau
{
    public int MaDe { get; set; }

    public int MaCH { get; set; }
}
