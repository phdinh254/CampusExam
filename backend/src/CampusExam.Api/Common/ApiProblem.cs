using System.Diagnostics;
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
}
