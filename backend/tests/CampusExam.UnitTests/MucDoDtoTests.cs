using System.Text.Json;
using CampusExam.Application.MucDo;
using Xunit;

namespace CampusExam.UnitTests;

public sealed class MucDoDtoTests
{
    [Fact]
    public void Serialize_with_web_defaults_keeps_expected_json_shape()
    {
        var danhSach = new[]
        {
            new MucDoDto(1, "Dễ")
        };

        var json = JsonSerializer.Serialize(
            danhSach,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        using var document = JsonDocument.Parse(json);
        var mucDo = document.RootElement[0];

        Assert.Equal(1, mucDo.GetProperty("maMD").GetByte());
        Assert.Equal("Dễ", mucDo.GetProperty("tenMD").GetString());
        Assert.False(mucDo.TryGetProperty("MaMD", out _));
        Assert.False(mucDo.TryGetProperty("TenMD", out _));
    }
}
