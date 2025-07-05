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
        Assert.That(result.error, Is.Null);
    }
}