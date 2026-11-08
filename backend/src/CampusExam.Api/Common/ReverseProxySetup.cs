using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace CampusExam.Api.Common;

/// <summary>
/// Đọc IP thật của người dùng từ X-Forwarded-For khi API chạy sau reverse proxy. Rate limit và
/// khóa đăng nhập tính theo IP; thiếu bước này mọi người dùng sẽ chung IP của proxy.
/// Chỉ tin header từ proxy khai báo trong "ReverseProxy:KnownProxies" (và loopback mặc định),
/// để kẻ tấn công không tự đặt X-Forwarded-For mà né hạn mức.
/// </summary>
public static class ReverseProxySetup
{
    public const string SectionName = "ReverseProxy";

    public static IServiceCollection AddCampusExamReverseProxy(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var proxies = section.GetSection("KnownProxies").Get<string[]>() ?? [];
        var networks = section.GetSection("KnownNetworks").Get<string[]>() ?? [];

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            // Chỉ lấy một bước: IP do proxy tin cậy gần nhất ghi vào.
            options.ForwardLimit = 1;

            foreach (var proxy in proxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            foreach (var network in networks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

        return services;
    }
}
