namespace SceneSplit.ImageCompression.Api.Helpers.Tests;

[TestFixture]
public class SizeConversionHelperTests
{
    [Test]
    public void ToMB_ConvertsBytesToMegabytes_Correctly()
    {
        // Arrange
        int bytes = 10 * 1024 * 1024;

        // Act
        int mb = SizeConversionHelper.ToMB(bytes);

        // Assert
        Assert.That(mb, Is.EqualTo(10));
    }

    [Test]
    public void ToMB_RoundsDownPartialMegabytes()
    {
        // Arrange
        int bytes = (10 * 1024 * 1024) + 500_000;

        // Act
        int mb = SizeConversionHelper.ToMB(bytes);

        // Assert
        Assert.That(mb, Is.EqualTo(10));
    }

    [Test]
    public void ToBytes_ConvertsMegabytesToBytes_Correctly()
    {
        // Arrange
        int mb = 15;

        // Act
        int bytes = SizeConversionHelper.ToBytes(mb);

        // Assert
        Assert.That(bytes, Is.EqualTo(15 * 1024 * 1024));
    }

    [Test]
    public void ToBytes_ZeroMegabytes_ReturnsZero()
    {
        // Arrange
        int mb = 0;

        // Act
        int bytes = SizeConversionHelper.ToBytes(mb);

        // Assert
        Assert.That(bytes, Is.Zero);
    }
}