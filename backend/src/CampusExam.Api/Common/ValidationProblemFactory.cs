using CampusExam.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace CampusExam.Api.Common;

/// <summary>
/// Tạo phản hồi 400 cho lỗi model binding/validation theo hợp đồng API_DESIGN.md mục 1.4:
/// có <c>code</c>, khóa <c>errors</c> trùng tên trường JSON (camelCase), không lộ tên kiểu .NET.
/// </summary>
public static class ValidationProblemFactory
{
    public const string ThongBaoGiaTriKhongHopLe = "Giá trị không hợp lệ.";

    public static IActionResult Create(ActionContext context)
    {
        var modelState = context.ModelState;
        var coLoiDocJson = modelState.Keys.Any(key => key.StartsWith('$'));
        var tenThamSoBody = context.ActionDescriptor.Parameters
            .Where(parameter => parameter.BindingInfo?.BindingSource?.Id == "Body")
            .Select(parameter => parameter.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var (key, entry) in modelState)
        {
            if (entry.Errors.Count == 0)
            {
                continue;
            }

            // Body không đọc được thì tham số body thành null và MVC báo thêm "field is required";
            // lỗi đó chỉ là hệ quả, đã có lỗi gốc ở khóa "$...".
            if (coLoiDocJson && tenThamSoBody.Contains(key))
            {
                continue;
            }

            var tenTruong = ChuanHoaKhoa(key);
            var thongBao = entry.Errors
                .Select(error => error.Exception is not null || key.StartsWith('$')
                    ? ThongBaoGiaTriKhongHopLe
                    : error.ErrorMessage)
                .Distinct()
                .ToArray();

            errors[tenTruong] = errors.TryGetValue(tenTruong, out var daCo)
                ? [.. daCo.Union(thongBao)]
                : thongBao;
        }

        var problemDetails = context.HttpContext.RequestServices
            .GetRequiredService<ProblemDetailsFactory>()
            .CreateProblemDetails(
                context.HttpContext,
                StatusCodes.Status400BadRequest,
                title: "Dữ liệu không hợp lệ");
        problemDetails.Extensions["errors"] = errors;
        problemDetails.Extensions["code"] = ErrorCodes.ValidationFailed;

        return new BadRequestObjectResult(problemDetails)
        {
            ContentTypes = { "application/problem+json" }
        };
    }

    /// <summary><c>$.trangThai</c> → <c>trangThai</c>, <c>TenKQHT</c> → <c>tenKQHT</c>, <c>$</c> → <c>body</c>.</summary>
    private static string ChuanHoaKhoa(string key)
    {
        var path = key.TrimStart('$').TrimStart('.');
        if (path.Length == 0)
        {
            return "body";
        }

        return string.Join('.', path.Split('.').Select(phan =>
            phan.Length == 0 ? phan : char.ToLowerInvariant(phan[0]) + phan[1..]));
    }
}
