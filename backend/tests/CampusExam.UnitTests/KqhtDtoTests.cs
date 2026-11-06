using System.Text.Json;
using CampusExam.Application.Common.Models;
using CampusExam.Application.Kqht;
using Xunit;

namespace CampusExam.UnitTests;

public sealed class KqhtDtoTests
{
    [Fact]
    public void Serialize_with_web_defaults_keeps_expected_json_shape()
    {
        var danhSach = new[]
        {
            new KqhtDto(1, "Nhớ", 1, null, TrangThaiSuDung.DangSuDung, "A")
        };

        var json = JsonSerializer.Serialize(
            danhSach,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        using var document = JsonDocument.Parse(json);
        var kqht = document.RootElement[0];

        Assert.Equal(1, kqht.GetProperty("maKQHT").GetInt32());
        Assert.Equal("Nhớ", kqht.GetProperty("tenKQHT").GetString());
        Assert.Equal(1, kqht.GetProperty("thuTu").GetByte());
        Assert.Equal(JsonValueKind.Null, kqht.GetProperty("moTa").ValueKind);
        Assert.Equal("dang_su_dung", kqht.GetProperty("trangThai").GetString());
        Assert.Equal("A", kqht.GetProperty("kyHieu").GetString());
    }
}
