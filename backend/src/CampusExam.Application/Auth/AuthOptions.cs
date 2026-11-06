namespace CampusExam.Application.Auth;

/// <summary>Cấu hình xác thực, đọc từ section "Auth". SigningKey đặt trong User Secrets.</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>Khóa ký HS256, tối thiểu 32 byte. Không commit vào repo.</summary>
    public string SigningKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = "CampusExam";

    public string Audience { get; set; } = "CampusExam";

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenHours { get; set; } = 12;

    public int RememberMeDays { get; set; } = 7;

    public int MaxFailedLogins { get; set; } = 5;

    public int LockoutMinutes { get; set; } = 15;

    public int LoginPermitsPerMinute { get; set; } = 10;

    /// <summary>Origin được phép gọi lam-moi/dang-xuat (chống CSRF), ví dụ http://localhost:5173.</summary>
    public string[] AllowedOrigins { get; set; } = [];

    public TimeSpan ThoiHanPhien(bool ghiNho) =>
        ghiNho ? TimeSpan.FromDays(RememberMeDays) : TimeSpan.FromHours(RefreshTokenHours);
}
