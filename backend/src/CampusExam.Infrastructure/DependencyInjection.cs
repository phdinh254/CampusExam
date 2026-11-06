using CampusExam.Application.Accounts;
using CampusExam.Application.Interfaces.Security;
using CampusExam.Application.Kqht;
using CampusExam.Application.MucDo;
using CampusExam.Application.QuestionImports;
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
        services.AddScoped<IMucDoQuery, EfMucDoQuery>();
        services.AddSingleton<IQuestionDocxParser, OpenXmlQuestionDocxParser>();
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddScoped<IInitialAdminService, EfInitialAdminService>();

        return services;
    }
}
