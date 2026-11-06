using System.Text.Json.Serialization;
using CampusExam.Application.Common.Json;

namespace CampusExam.Application.Common.Models;

/// <summary>Trạng thái 0/1 của KQHOCTAP.TRANGTHAI và CAUHOI.TRANGTHAI.</summary>
[JsonConverter(typeof(EnumChuoiConverter<TrangThaiSuDung>))]
public enum TrangThaiSuDung : short
{
    [JsonStringEnumMemberName("ngung_su_dung")]
    NgungSuDung = 0,

    [JsonStringEnumMemberName("dang_su_dung")]
    DangSuDung = 1
}
