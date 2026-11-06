namespace CampusExam.Api.Authorization;

/// <summary>Tên claim trong access token (không ánh xạ sang ClaimTypes dài của .NET).</summary>
public static class CampusClaims
{
    public const string MaTaiKhoan = "sub";
    public const string TenDangNhap = "name";
    public const string VaiTro = "role";
    public const string Msts = "msts";
    public const string MaBaoMat = "sstamp";
    public const string PhaiDoiMatKhau = "doi_mk";
}

public static class Policies
{
    public const string AdminOnly = "AdminOnly";
    public const string CandidateOnly = "CandidateOnly";
}

public static class RateLimitPolicies
{
    public const string DangNhap = "dang-nhap";
}

/// <summary>
/// Cho phép gọi endpoint khi tài khoản còn bị bắt buộc đổi mật khẩu (chỉ dùng cho /api/xac-thuc).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ChoPhepKhiPhaiDoiMatKhauAttribute : Attribute;
