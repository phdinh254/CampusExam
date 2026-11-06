using System.Text.Json.Serialization;

namespace CampusExam.Application.Common.Json;

/// <summary>
/// Ghi/đọc enum bằng tên khai báo qua <see cref="JsonStringEnumMemberNameAttribute"/>
/// và từ chối giá trị số, để API không lộ hoặc nhận mã số trạng thái của database.
/// </summary>
public sealed class EnumChuoiConverter<TEnum>() : JsonStringEnumConverter<TEnum>(
    namingPolicy: null,
    allowIntegerValues: false)
    where TEnum : struct, Enum;
