using System.Security.Claims;
using System.Text;
using CampusExam.Application.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CampusExam.Api.Authorization;

public sealed class JwtTokenService(IOptions<AuthOptions> options, TimeProvider timeProvider) : ITokenService
{
    private readonly JsonWebTokenHandler _handler = new();

    public (string Token, DateTime HetHan) TaoAccessToken(ThongTinToken thongTin)
    {
        var auth = options.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var hetHan = now.AddMinutes(auth.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(CampusClaims.MaTaiKhoan, thongTin.Mtk.ToString()),
            new(CampusClaims.TenDangNhap, thongTin.TenDN),
            new(CampusClaims.VaiTro, thongTin.VaiTro),
            new(CampusClaims.MaBaoMat, thongTin.MaBaoMat.ToString()),
            new(CampusClaims.PhaiDoiMatKhau, thongTin.PhaiDoiMatKhau ? "1" : "0")
        };
        if (thongTin.Msts is not null)
        {
            claims.Add(new Claim(CampusClaims.Msts, thongTin.Msts));
        }

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = auth.Issuer,
            Audience = auth.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = hetHan,
            SigningCredentials = new SigningCredentials(TaoKhoaKy(auth), SecurityAlgorithms.HmacSha256)
        });

        return (token, hetHan);
    }

    public static SymmetricSecurityKey TaoKhoaKy(AuthOptions auth)
    {
        var bytes = Encoding.UTF8.GetBytes(auth.SigningKey ?? string.Empty);
        if (bytes.Length < 32)
        {
            throw new InvalidOperationException(
                "Chưa cấu hình Auth:SigningKey (tối thiểu 32 byte). Đặt bằng User Secrets: " +
                "dotnet user-secrets set \"Auth:SigningKey\" \"<chuỗi ngẫu nhiên>\" --project src/CampusExam.Api");
        }

        return new SymmetricSecurityKey(bytes);
    }
}
