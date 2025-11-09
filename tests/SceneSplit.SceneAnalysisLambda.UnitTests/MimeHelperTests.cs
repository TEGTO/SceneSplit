namespace SceneSplit.SceneAnalysisLambda.Tests;

[TestFixture]
public class MimeHelperTests
{
    [TestCase(null, "file.jpg", "image/jpeg")]
    [TestCase("", "file.jpg", "image/jpeg")]
    [TestCase("   ", "file.jpg", "image/jpeg")]
    [TestCase("image/jpg", "file.jpg", "image/jpeg")]
    [TestCase("IMAGE/JPG", "file.JPG", "image/jpeg")]
    [TestCase("image/pjpeg", "file.jpeg", "image/jpeg")]
    [TestCase("image/x-png", "file.png", "image/png")]
    [TestCase("image/png", "file.png", "image/png")]
    [TestCase("IMAGE/PNG", "file.PNG", "image/png")]
    [TestCase("image/jpeg", "file.jpeg", "image/jpeg")]
    [TestCase("image/webp", "file.webp", "image/webp")]
    [TestCase("image/gif", "file.gif", "image/gif")]
    [TestCase("application/octet-stream", "file.jpg", "image/jpeg")]
    [TestCase("application/octet-stream", "file.png", "image/png")]
    [TestCase("application/octet-stream", "file.jpeg", "image/jpeg")]
    [TestCase("application/octet-stream", "file.webp", "image/webp")]
    [TestCase("application/octet-stream", "file.gif", "image/gif")]
    [TestCase("application/octet-stream", "file.unknown", "image/jpeg")]
    public void NormalizeMime_ReturnsExpected(string? inputMime, string key, string expected)
    {
        var actual = MimeHelper.NormalizeMime(inputMime, key);
        Assert.That(actual, Is.EqualTo(expected));
    }

    [Test]
    public void NormalizeMime_UnknownMimeUnknownExtension_FallsBackToJpeg()
    {
        var actual = MimeHelper.NormalizeMime("application/x-custom", "file.bin");
        Assert.That(actual, Is.EqualTo("image/jpeg"));
    }

    [Test]
    public void NormalizeMime_StripsWhitespaceAndCase()
    {
        var actual = MimeHelper.NormalizeMime("  IMAGE/JPG  ", "ignored.png");
        Assert.That(actual, Is.EqualTo("image/jpeg"));
    }

    [Test]
    public void NormalizeMime_PrefersHeaderOverExtensionWhenRecognized()
    {
        var actual = MimeHelper.NormalizeMime("image/gif", "picture.png");
        Assert.That(actual, Is.EqualTo("image/gif"));
    }
}