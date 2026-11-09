using CampusExam.Application.Accounts;
using CampusExam.Application.Auth;
using CampusExam.Application.CauHoi;
using CampusExam.Infrastructure.Services;
using CampusExam.Application.Interfaces.Security;
using CampusExam.Application.Kqht;
using CampusExam.Application.MucDo;
using CampusExam.Application.QuestionImports;
using CampusExam.Application.ThiSinh;
using CampusExam.Infrastructure.Commands;
using CampusExam.Infrastructure.Imports;
using CampusExam.Infrastructure.Queries;
using CampusExam.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace CampusExam.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IKqhtCommandService, EfKqhtCommandService>();
        services.AddScoped<IKqhtQuery, EfKqhtQuery>();
        services.AddScoped<ICauHoiQuery, EfCauHoiQuery>();
        services.AddScoped<ICauHoiCommandService, EfCauHoiCommandService>();
        services.AddScoped<IThiSinhQuery, EfThiSinhQuery>();
        services.AddScoped<IThiSinhCommandService, EfThiSinhCommandService>();
        services.AddScoped<IMucDoQuery, EfMucDoQuery>();
        services.AddSingleton<IQuestionDocxParser>(new OpenXmlQuestionDocxParser(GioiHanTepDocx.MacDinh));
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddScoped<IInitialAdminService, EfInitialAdminService>();
        services.AddScoped<IAuthService, EfAuthService>();
        services.AddSingleton(TimeProvider.System);
        services.AddMemoryCache();

        return services;
    }
}
