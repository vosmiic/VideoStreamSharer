using VideoStreamBackend.Helpers;

namespace UnitTests.Helpers;

[TestFixture]
public class YtDlpHelperTests {
    private YtDlpHelper _ytDlpHelper;
    
    [SetUp]
    public void SetUp() {
        _ytDlpHelper = new YtDlpHelper();
    }

    [TestCase("")]
    public async Task GetInfoTest(string url) {
        Uri uri = new(url);
        var result = await _ytDlpHelper.GetVideoInfo(uri);
        Assert.That(result.success, Is.True);
        Assert.That(result.videoInfo, Is.Not.Null);
        Assert.That(result.videoInfo.Title, Is.Not.Empty);
        Assert.That(result.error, Is.Null);
    }

    [TestCase("")]
    public async Task GetVideoUrlTest(string url) {
        Uri uri = new(url);
        var result = await _ytDlpHelper.GetVideoUrls(uri);
        Assert.That(result.success, Is.True);
        Assert.That(result.urls, Is.Not.Empty);
        Assert.That(result.urls.First(), Is.Not.Empty);
        Assert.That(result.error, Is.Null);
    }
}