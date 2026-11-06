namespace CampusExam.Application.Auth;

public interface IAuthService
{
    Task<DangNhapResult> DangNhapAsync(DangNhapRequest request, NguCanhYeuCau nguCanh, CancellationToken cancellationToken);

    /// <summary>Xoay vòng refresh token; trả null khi token không hợp lệ, hết hạn hoặc đã thu hồi.</summary>
    Task<PhienDaCap?> LamMoiAsync(string refreshToken, NguCanhYeuCau nguCanh, CancellationToken cancellationToken);

    Task DangXuatAsync(string refreshToken, NguCanhYeuCau nguCanh, CancellationToken cancellationToken);

    Task<TaiKhoanHienTaiDto?> LayTaiKhoanAsync(int mtk, CancellationToken cancellationToken);

    /// <summary>Đổi mật khẩu, thu hồi mọi phiên cũ và cấp phiên mới.</summary>
    Task<DoiMatKhauResult> DoiMatKhauAsync(
        int mtk,
        DoiMatKhauRequest request,
        string? refreshTokenHienTai,
        NguCanhYeuCau nguCanh,
        CancellationToken cancellationToken);

    /// <summary>Access token còn dùng được: tài khoản hoạt động và mã bảo mật chưa đổi.</summary>
    Task<bool> AccessTokenConHieuLucAsync(int mtk, Guid maBaoMat, CancellationToken cancellationToken);
}

public interface ITokenService
{
    (string Token, DateTime HetHan) TaoAccessToken(ThongTinToken thongTin);
}
