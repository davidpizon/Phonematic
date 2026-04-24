using System.Globalization;
using Phonematic.Converters;

namespace Phonematic.Tests;

public class ConverterTests
{
    [Theory]
    [InlineData(0.0, "0%")]
    [InlineData(0.5, "50%")]
    [InlineData(1.0, "100%")]
    [InlineData(0.753, "75%")]
    public void PercentageConverter_FormatsCorrectly(double input, string expected)
    {
        var result = PercentageConverter.Instance.Convert(input, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(500L, "500 B")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(1048576L, "1.0 MB")]
    [InlineData(1572864L, "1.5 MB")]
    [InlineData(1073741824L, "1.00 GB")]
    public void FileSizeConverter_FormatsCorrectly(long input, string expected)
    {
        var result = FileSizeConverter.Instance.Convert(input, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(45.0, "45s")]
    [InlineData(125.0, "2m 5s")]
    [InlineData(3725.0, "1h 2m 5s")]
    public void DurationConverter_FormatsCorrectly(double seconds, string expected)
    {
        var result = DurationConverter.Instance.Convert(seconds, typeof(string), null, CultureInfo.InvariantCulture);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void InverseBoolConverter_InvertsBool()
    {
        Assert.Equal(false, InverseBoolConverter.Instance.Convert(true, typeof(bool), null, CultureInfo.InvariantCulture));
        Assert.Equal(true, InverseBoolConverter.Instance.Convert(false, typeof(bool), null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void InverseBoolConverter_ConvertBack_InvertsBool()
    {
        Assert.Equal(false, InverseBoolConverter.Instance.ConvertBack(true, typeof(bool), null, CultureInfo.InvariantCulture));
        Assert.Equal(true, InverseBoolConverter.Instance.ConvertBack(false, typeof(bool), null, CultureInfo.InvariantCulture));
    }
}
