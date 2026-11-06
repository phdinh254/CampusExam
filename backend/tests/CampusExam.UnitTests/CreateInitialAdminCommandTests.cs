using System.ComponentModel.DataAnnotations;
using CampusExam.Application.Accounts;
using Xunit;

namespace CampusExam.UnitTests;

public sealed class CreateInitialAdminCommandTests
{
    [Fact]
    public void Valid_command_passes_validation()
    {
        Assert.Empty(Validate(Create("admin", "Quản trị viên", "MatKhau2026")));
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("tên có dấu")]
    [InlineData("admin@truong")]
    public void Invalid_ten_dn_is_rejected(string tenDN)
    {
        AssertInvalid(Create(tenDN, "Quản trị viên", "MatKhau2026"), nameof(CreateInitialAdminCommand.TenDN));
    }

    [Theory]
    [InlineData("Ngan1")]
    [InlineData("chikhongcoso")]
    [InlineData("1234567890")]
    public void Weak_password_is_rejected(string matKhau)
    {
        AssertInvalid(Create("admin", "Quản trị viên", matKhau), nameof(CreateInitialAdminCommand.MatKhau));
    }

    [Fact]
    public void Password_containing_ten_dn_is_rejected()
    {
        AssertInvalid(Create("quantri", "Quản trị viên", "QuanTri2026"), nameof(CreateInitialAdminCommand.MatKhau));
    }

    [Fact]
    public void Blank_ho_ten_is_rejected()
    {
        AssertInvalid(Create("admin", "   ", "MatKhau2026"), nameof(CreateInitialAdminCommand.HoTen));
    }

    private static CreateInitialAdminCommand Create(string tenDN, string hoTen, string matKhau) =>
        new() { TenDN = tenDN, HoTen = hoTen, MatKhau = matKhau };

    private static void AssertInvalid(CreateInitialAdminCommand command, string member)
    {
        Assert.Contains(Validate(command), error => error.MemberNames.Contains(member));
    }

    private static List<ValidationResult> Validate(CreateInitialAdminCommand command)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(command, new ValidationContext(command), errors, validateAllProperties: true);
        return errors;
    }
}
