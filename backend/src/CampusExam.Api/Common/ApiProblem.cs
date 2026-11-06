using System.Diagnostics;
using CampusExam.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace CampusExam.Api.Common;

/// <summary>Tạo ProblemDetails có <c>code</c> ổn định và <c>traceId</c> (docs/API_DESIGN.md mục 1.4).</summary>
public static class ApiProblem
{
    public static ProblemDetails Create(
        HttpContext? httpContext,
        int status,
        string title,
        string code,
        string? detail = null)
    {
        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail
        };
        problemDetails.Extensions["code"] = code;

        var traceId = Activity.Current?.Id ?? httpContext?.TraceIdentifier;
        if (traceId is not null)
        {
            problemDetails.Extensions["traceId"] = traceId;
        }

        return problemDetails;
    }

    /// <summary>Lỗi 400 kèm lỗi theo trường; khóa là tên trường JSON (camelCase).</summary>
    public static ProblemDetails Validation(
        HttpContext? httpContext,
        IDictionary<string, string[]> errors,
        string code = ErrorCodes.ValidationFailed,
        string title = "Dữ liệu không hợp lệ")
    {
        var problemDetails = Create(httpContext, StatusCodes.Status400BadRequest, title, code);
        problemDetails.Extensions["errors"] = errors;
        return problemDetails;
    }
}
