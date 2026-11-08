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

    /// <summary>
    /// Số request đăng nhập/đổi mật khẩu mỗi phút cho một IP. Đủ rộng cho phòng máy dùng chung
    /// IP (NAT) vào đầu giờ thi; dò mật khẩu từng người đã bị chặn bởi khóa theo tên + IP.
    /// </summary>
    public int LoginPermitsPerMinute { get; set; } = 60;

    /// <summary>
    /// Số giây cache kết quả kiểm tra mã bảo mật của access token (tránh truy vấn database mỗi request).
    /// Đổi mật khẩu trên cùng instance xóa cache ngay; thay đổi từ nơi khác (instance khác, sửa trực tiếp
    /// database) có hiệu lực chậm tối đa chừng này giây. 0 = tắt cache.
    /// </summary>
    public int TokenCheckCacheSeconds { get; set; } = 30;

    /// <summary>Origin được phép gọi lam-moi/dang-xuat (chống CSRF), ví dụ http://localhost:5173.</summary>
    public string[] AllowedOrigins { get; set; } = [];

    public TimeSpan ThoiHanPhien(bool ghiNho) =>
        ghiNho ? TimeSpan.FromDays(RememberMeDays) : TimeSpan.FromHours(RefreshTokenHours);
}
