using VideoStreamBackend.Helpers;

namespace UnitTests.Helpers;

[TestFixture]
public class QueueHelperTests {
    
    [TestCase("https://example.com?v=123&list=234&start_radio=1&pp=345", "https://example.com/?v=123")]
    public void GetYouTubeUrlTests(string url, string expected) {
        var result = QueueHelper.GetYouTubeUrl(new Uri(url));
        Assert.That(result.ToString(), Is.EqualTo(expected));
    }
}