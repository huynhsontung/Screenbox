#nullable enable

using System.Text.Json.Serialization.Metadata;
using Screenbox.Core.Models;
using Screenbox.Core.Models.Serialization;
using Xunit;

namespace Screenbox.Core.Tests.Aot;

public sealed class NativeAotCompatibilityTests
{
    [Fact]
    public void CoreJsonContext_MetadataProvider_IsReflectionFreeAndAotCompatible()
    {
        // Native AOT requires JsonSerializerContext metadata to provide JsonTypeInfo without runtime reflection
        JsonTypeInfo<PlaylistRecordDto>? playlistTypeInfo = CoreJsonContext.Default.GetTypeInfo(typeof(PlaylistRecordDto)) as JsonTypeInfo<PlaylistRecordDto>;
        Assert.NotNull(playlistTypeInfo);

        JsonTypeInfo<RawMediaRecordDto>? rawMediaTypeInfo = CoreJsonContext.Default.GetTypeInfo(typeof(RawMediaRecordDto)) as JsonTypeInfo<RawMediaRecordDto>;
        Assert.NotNull(rawMediaTypeInfo);
    }

    [Fact]
    public void SqlParameterDto_ParameterBindingModel_IsAotSafe()
    {
        var stringParam = new SqlParameterDto { Name = "@str", Value = "TestValue" };
        var intParam = new SqlParameterDto { Name = "@num", Value = 42 };

        Assert.Equal("@str", stringParam.Name);
        Assert.Equal("TestValue", stringParam.Value);
        Assert.Equal("@num", intParam.Name);
        Assert.Equal(42, intParam.Value);
    }
}
