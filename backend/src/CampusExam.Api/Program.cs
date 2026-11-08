using System.Diagnostics;
using CampusExam.Api.Authorization;
using CampusExam.Api.Cli;
using CampusExam.Api.Common;
using CampusExam.Infrastructure;
using CampusExam.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("CampusExam")
    ?? throw new InvalidOperationException(
        "Chưa cấu hình ConnectionStrings:CampusExam.");

builder.Services.AddDbContext<CampusExamDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddInfrastructure();
builder.Services.AddCampusExamAuth(builder.Configuration);
builder.Services.AddCampusExamReverseProxy(builder.Configuration);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = ValidationProblemFactory.Create;
    });
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    };
});

if (BootstrapAdminCli.IsRequested(args))
{
    builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
}

var app = builder.Build();

if (BootstrapAdminCli.IsRequested(args))
{
    return await BootstrapAdminCli.RunAsync(app.Services, app.Configuration);
}

// Đầu tiên: mọi middleware sau (rate limit, HTTPS redirect, nhật ký) thấy IP và scheme thật.
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();
app.UseCampusExamAuth();
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    // /openapi/v1.json — nguồn để frontend sinh kiểu TypeScript; không mở ở production.
    app.MapOpenApi().AllowAnonymous();
}

app.Run();
return 0;

public partial class Program;
