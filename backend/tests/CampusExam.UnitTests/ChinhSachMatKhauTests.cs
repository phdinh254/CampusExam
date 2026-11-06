using CampusExam.Application.Accounts;
using Xunit;

namespace CampusExam.UnitTests;

public sealed class ChinhSachMatKhauTests
{
    private static readonly DateOnly NgaySinh = new(2005, 3, 14);

    [Theory]
    [InlineData(2005, 3, 14, "14032005")]
    [InlineData(1999, 12, 1, "01121999")]
    [InlineData(2004, 2, 29, "29022004")]
    public void Mat_khau_mac_dinh_la_ngay_sinh_ddMMyyyy(int nam, int thang, int ngay, string expected)
    {
        Assert.Equal(expected, ChinhSachMatKhau.MatKhauMacDinh(new DateOnly(nam, thang, ngay)));
    }

    [Fact]
    public void Mat_khau_manh_hop_le()
    {
        Assert.Empty(ChinhSachMatKhau.KiemTra("ThiSinh2026", "SV001", NgaySinh));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ab1")]
    [InlineData("chikhongcoso")]
    [InlineData("1234567890")]
    [InlineData("SV001abc9")]
    [InlineData("sv001abc9")]
    public void Mat_khau_yeu_bi_tu_choi(string matKhau)
    {
        Assert.NotEmpty(ChinhSachMatKhau.KiemTra(matKhau, "SV001", NgaySinh));
    }

    [Theory]
    [InlineData("14032005")]
    [InlineData("a14032005")]
    public void Mat_khau_chua_ngay_sinh_bi_tu_choi(string matKhau)
    {
        Assert.Contains(
            "Mật khẩu không được chứa ngày sinh.",
            ChinhSachMatKhau.KiemTra(matKhau, "SV001", NgaySinh));
    }

    [Fact]
    public void Khong_kiem_tra_ngay_sinh_khi_khong_co()
    {
        Assert.Empty(ChinhSachMatKhau.KiemTra("a14032005", "admin"));
    }
}
