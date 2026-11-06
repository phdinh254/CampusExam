using System.Text.Json;
using System.Threading.RateLimiting;
using CampusExam.Api.Common;
using CampusExam.Application.Auth;
using CampusExam.Application.Common;
using CampusExam.Domain.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CampusExam.Api.Authorization;

public static class AuthSetup
{
    public static IServiceCollection AddCampusExamAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));
        services.AddSingleton<ITokenService, JwtTokenService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        // Cấu hình lúc chạy để khóa ký chỉ bắt buộc khi thật sự xác thực (lệnh bootstrap-admin không cần).
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthOptions>>((jwt, authOptions) =>
            {
                var auth = authOptions.Value;
                jwt.MapInboundClaims = false;
                jwt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = auth.Issuer,
                    ValidAudience = auth.Audience,
                    IssuerSigningKey = JwtTokenService.TaoKhoaKy(auth),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = CampusClaims.TenDangNhap,
                    RoleClaimType = CampusClaims.VaiTro,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                jwt.Events = new JwtBearerEvents
                {
                    OnTokenValidated = KiemTraMaBaoMatAsync,
                    OnChallenge = async context =>
                    {
                        context.HandleResponse();
                        var hetHan = context.AuthenticateFailure is not null
                                     || context.Request.Headers.Authorization.Count > 0;
                        context.Response.Headers.WWWAuthenticate = "Bearer";
                        await GhiLoiAsync(context.HttpContext, StatusCodes.Status401Unauthorized,
                            hetHan ? "Phiên đăng nhập đã hết hạn" : "Chưa đăng nhập",
                            hetHan ? ErrorCodes.AuthTokenExpired : ErrorCodes.AuthUnauthenticated);
                    },
                    OnForbidden = context => GhiLoiAsync(context.HttpContext, StatusCodes.Status403Forbidden,
                        "Bạn không có quyền thực hiện thao tác này", ErrorCodes.Forbidden)
                };
            });

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.AdminOnly, policy => policy.RequireRole(TaiKhoan.VaiTroAdmin))
            .AddPolicy(Policies.CandidateOnly, policy => policy
                .RequireRole(TaiKhoan.VaiTroThiSinh)
                .RequireClaim(CampusClaims.Msts));

        services.AddRateLimiter(options =>
        {
            options.OnRejected = async (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                }

                await GhiLoiAsync(context.HttpContext, StatusCodes.Status429TooManyRequests,
                    "Bạn thao tác quá nhanh. Vui lòng thử lại sau ít phút.", ErrorCodes.RateLimited);
            };
            options.AddPolicy(RateLimitPolicies.DangNhap, httpContext =>
            {
                var auth = httpContext.RequestServices.GetRequiredService<IOptions<AuthOptions>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "khong-ro",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = auth.LoginPermitsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
        });

        return services;
    }

    /// <summary>Thứ tự: rate limit → xác thực → chặn tài khoản phải đổi mật khẩu → phân quyền.</summary>
    public static WebApplication UseCampusExamAuth(this WebApplication app)
    {
        app.UseRateLimiter();
        app.UseAuthentication();
        app.Use(ChanKhiPhaiDoiMatKhauAsync);
        app.UseAuthorization();
        return app;
    }

    private static async Task KiemTraMaBaoMatAsync(TokenValidatedContext context)
    {
        var principal = context.Principal!;
        if (!int.TryParse(principal.FindFirst(CampusClaims.MaTaiKhoan)?.Value, out var mtk)
            || !Guid.TryParse(principal.FindFirst(CampusClaims.MaBaoMat)?.Value, out var maBaoMat))
        {
            context.Fail("Token thiếu claim bắt buộc.");
            return;
        }

        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
        if (!await authService.AccessTokenConHieuLucAsync(mtk, maBaoMat, context.HttpContext.RequestAborted))
        {
            context.Fail("Tài khoản đã đổi mật khẩu, bị khóa hoặc không còn tồn tại.");
        }
    }

    private static async Task ChanKhiPhaiDoiMatKhauAsync(HttpContext context, RequestDelegate next)
    {
        var phaiDoi = context.User.Identity?.IsAuthenticated == true
                      && context.User.FindFirst(CampusClaims.PhaiDoiMatKhau)?.Value == "1";
        var duocPhep = context.GetEndpoint()?.Metadata.GetMetadata<ChoPhepKhiPhaiDoiMatKhauAttribute>() is not null;

        if (phaiDoi && !duocPhep)
        {
            await GhiLoiAsync(context, StatusCodes.Status403Forbidden,
                "Bạn cần đổi mật khẩu trước khi tiếp tục", ErrorCodes.AuthPasswordChangeRequired);
            return;
        }

        await next(context);
    }

    private static Task GhiLoiAsync(HttpContext context, int status, string title, string code)
    {
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(
            ApiProblem.Create(context, status, title, code),
            (JsonSerializerOptions?)null,
            "application/problem+json");
    }
}
