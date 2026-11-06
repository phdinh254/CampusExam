using CampusExam.Application.Common.Models;
using System.ComponentModel.DataAnnotations;
using CampusExam.Application.Kqht;
using Xunit;

namespace CampusExam.UnitTests;

public sealed class CreateKqhtCommandTests
{
    [Fact]
    public void Valid_command_passes_validation()
    {
        var command = new CreateKqhtCommand
        {
            TenKQHT = "Nhớ",
            ThuTu = 1,
            MoTa = "Mô tả",
            TrangThai = TrangThaiSuDung.DangSuDung,
            KyHieu = "A"
        };

        var errors = Validate(command);

        Assert.Empty(errors);
    }

    [Fact]
    public void New_kqht_defaults_to_dang_su_dung()
    {
        Assert.Equal(TrangThaiSuDung.DangSuDung, new CreateKqhtCommand().TrangThai);
    }

    [Fact]
    public void Invalid_command_reports_all_business_fields()
    {
        var command = new CreateKqhtCommand
        {
            TenKQHT = " ",
            ThuTu = 0,
            MoTa = new string('x', 1001),
            TrangThai = (TrangThaiSuDung)2,
            KyHieu = "AB"
        };

        var errors = Validate(command);
        var memberNames = errors
            .SelectMany(error => error.MemberNames)
            .ToHashSet();

        Assert.Contains(nameof(CreateKqhtCommand.TenKQHT), memberNames);
        Assert.Contains(nameof(CreateKqhtCommand.ThuTu), memberNames);
        Assert.Contains(nameof(CreateKqhtCommand.MoTa), memberNames);
        Assert.Contains(nameof(CreateKqhtCommand.TrangThai), memberNames);
        Assert.Contains(nameof(CreateKqhtCommand.KyHieu), memberNames);
    }

    [Theory]
    [InlineData(1, "A")]
    [InlineData(2, "B")]
    [InlineData(4, "d")]
    [InlineData(26, "Z")]
    public void KyHieu_matching_ThuTu_passes_validation(byte thuTu, string kyHieu)
    {
        var errors = Validate(CreateCommand(thuTu, kyHieu));

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(1, "B")]
    [InlineData(2, "A")]
    [InlineData(3, "D")]
    public void KyHieu_not_matching_ThuTu_is_rejected(byte thuTu, string kyHieu)
    {
        var errors = Validate(CreateCommand(thuTu, kyHieu));

        var error = Assert.Single(errors);
        Assert.Contains(nameof(CreateKqhtCommand.KyHieu), error.MemberNames);
        Assert.Contains(nameof(CreateKqhtCommand.ThuTu), error.MemberNames);
    }

    [Theory]
    [InlineData(27)]
    [InlineData(255)]
    public void ThuTu_above_26_is_rejected(byte thuTu)
    {
        var errors = Validate(CreateCommand(thuTu, "Z"));

        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(CreateKqhtCommand.ThuTu)));
    }

    private static CreateKqhtCommand CreateCommand(byte thuTu, string kyHieu) =>
        new()
        {
            TenKQHT = "KQHT",
            ThuTu = thuTu,
            TrangThai = TrangThaiSuDung.DangSuDung,
            KyHieu = kyHieu
        };

    private static IReadOnlyList<ValidationResult> Validate(
        CreateKqhtCommand command)
    {
        var errors = new List<ValidationResult>();
        Validator.TryValidateObject(
            command,
            new ValidationContext(command),
            errors,
            validateAllProperties: true);

        return errors;
    }
}
