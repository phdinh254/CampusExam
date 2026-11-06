using System.ComponentModel.DataAnnotations;
using System.Text;
using CampusExam.Application.Accounts;

namespace CampusExam.Api.Cli;

/// <summary>
/// dotnet run -- bootstrap-admin --ten-dn admin --ho-ten "Quản trị viên"
/// Mật khẩu lấy từ biến môi trường CAMPUSEXAM_ADMIN_PASSWORD hoặc nhập ẩn trên console.
/// </summary>
internal static class BootstrapAdminCli
{
    public const string CommandName = "bootstrap-admin";
    private const string PasswordVariable = "CAMPUSEXAM_ADMIN_PASSWORD";

    public static bool IsRequested(string[] args) =>
        args.Length > 0 && args[0].Equals(CommandName, StringComparison.OrdinalIgnoreCase);

    public static async Task<int> RunAsync(IServiceProvider services, IConfiguration configuration)
    {
        Console.OutputEncoding = Encoding.UTF8;

        var tenDN = configuration["ten-dn"];
        var hoTen = configuration["ho-ten"];
        if (string.IsNullOrWhiteSpace(tenDN) || string.IsNullOrWhiteSpace(hoTen))
        {
            Console.Error.WriteLine($"Cách dùng: {CommandName} --ten-dn <tên đăng nhập> --ho-ten \"<họ tên>\"");
            return 1;
        }

        var matKhau = Environment.GetEnvironmentVariable(PasswordVariable) ?? ReadPasswordTwice();
        if (matKhau is null)
        {
            Console.Error.WriteLine("Hai lần nhập mật khẩu không khớp.");
            return 1;
        }

        var command = new CreateInitialAdminCommand { TenDN = tenDN, HoTen = hoTen, MatKhau = matKhau };
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(command, new ValidationContext(command), errors, validateAllProperties: true))
        {
            foreach (var error in errors)
            {
                Console.Error.WriteLine($"- {error.ErrorMessage}");
            }

            return 1;
        }

        await using var scope = services.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IInitialAdminService>();
        var result = await service.CreateAsync(command, CancellationToken.None);

        switch (result.Status)
        {
            case CreateInitialAdminStatus.Created:
                Console.WriteLine($"Đã tạo tài khoản quản trị '{tenDN.Trim().ToLowerInvariant()}' (MTK = {result.MaTK}).");
                return 0;
            case CreateInitialAdminStatus.AdminAlreadyExists:
                Console.Error.WriteLine("Hệ thống đã có tài khoản quản trị; lệnh này chỉ dùng cho lần khởi tạo đầu tiên.");
                return 2;
            default:
                Console.Error.WriteLine($"Tên đăng nhập '{tenDN}' đã được dùng.");
                return 2;
        }
    }

    private static string? ReadPasswordTwice()
    {
        var first = ReadPassword("Mật khẩu: ");
        var second = ReadPassword("Nhập lại mật khẩu: ");
        return first == second ? first : null;
    }

    private static string ReadPassword(string prompt)
    {
        Console.Write(prompt);
        if (Console.IsInputRedirected)
        {
            // PowerShell có thể chèn BOM vào đầu luồng nhập khi pipe.
            var line = (Console.ReadLine() ?? string.Empty).TrimStart('﻿');
            Console.WriteLine();
            return line;
        }

        var buffer = new StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                return buffer.ToString();
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (buffer.Length > 0)
                {
                    buffer.Length--;
                }
            }
            else if (!char.IsControl(key.KeyChar))
            {
                buffer.Append(key.KeyChar);
            }
        }
    }
}
