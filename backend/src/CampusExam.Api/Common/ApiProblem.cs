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

    /// <summary>Đổi lỗi nghiệp vụ từ tầng Application thành phản hồi ProblemDetails.</summary>
    public static ObjectResult FromLoi(HttpContext httpContext, LoiNghiepVu loi)
    {
        var (status, problem) = loi.Loai switch
        {
            LoaiLoi.KhongHopLe => (
                StatusCodes.Status400BadRequest,
                Validation(httpContext, new Dictionary<string, string[]>
                {
                    [loi.Truong ?? "body"] = [loi.ChiTiet ?? loi.TieuDe]
                }, loi.Code, loi.TieuDe)),
            LoaiLoi.KhongTimThay => (
                StatusCodes.Status404NotFound,
                Create(httpContext, StatusCodes.Status404NotFound, loi.TieuDe, loi.Code, loi.ChiTiet)),
            LoaiLoi.HetHan => (
                StatusCodes.Status410Gone,
                Create(httpContext, StatusCodes.Status410Gone, loi.TieuDe, loi.Code, loi.ChiTiet)),
            _ => (
                StatusCodes.Status409Conflict,
                Create(httpContext, StatusCodes.Status409Conflict, loi.TieuDe, loi.Code, loi.ChiTiet))
        };

        return new ObjectResult(problem)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
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
