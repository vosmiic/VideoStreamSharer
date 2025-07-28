using System.Text;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using Moq;
using VideoStreamBackend.Helpers;
using VideoStreamBackend.Interfaces;
using VideoStreamBackend.Models.YtDlp;
using CommandResult = VideoStreamBackend.Interfaces.CommandResult;

namespace UnitTests.Helpers;

[TestFixture]
public class YtDlpHelperTests {
    private Mock<ICliWrapper> cliWrapper;
    
    [SetUp]
    public void SetUp() {
        cliWrapper = new Mock<ICliWrapper>();
    }

    [Test]
    public async Task GetInfoTest() {
        cliWrapper.Setup(cliwrapper => cliwrapper.ExecuteBufferedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PipeTarget>(), It.IsAny<PipeTarget>())).Returns(Task.FromResult(new CommandResult {
            IsSuccess = true
        }));
        StringBuilder standardOutput = new StringBuilder();
        StringBuilder errorOutput = new StringBuilder();

        standardOutput.Append(JsonSerializer.Serialize(new VideoInfo {
            Title = "title"
        }));

        YtDlpHelper ytDlpHelper = new YtDlpHelper(cliWrapper.Object);
        Uri uri = new("https://test.com");
        var result = await ytDlpHelper.GetVideoInfo(uri, standardOutput, errorOutput);
        Assert.That(result.success, Is.True);
        Assert.That(result.videoInfo, Is.Not.Null);
        Assert.That(result.videoInfo.Title, Is.Not.Empty);
        Assert.That(result.error, Is.Null);
    }

    [Test]
    public async Task GetVideoUrlTest() {
        cliWrapper.Setup(cliwrapper => cliwrapper.ExecuteBufferedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PipeTarget>(), It.IsAny<PipeTarget>())).Returns(Task.FromResult(new CommandResult {
            IsSuccess = true,
        }));
        StringBuilder standardOutput = new StringBuilder();
        StringBuilder errorOutput = new StringBuilder();
        long expiry = 1753479377;
        string[] stringArray = [ $"https://test.com?expire={expiry}", "https://test.com?exprie=1" ];

        standardOutput.AppendJoin(Environment.NewLine, stringArray);
        
        YtDlpHelper ytDlpHelper = new YtDlpHelper(cliWrapper.Object);
        var result = await ytDlpHelper.GetVideoUrls(new Uri("https://test.com"), standardOutput, errorOutput);
        Assert.That(result.success, Is.True);
        Assert.That(result.urls, Is.Not.Empty);
        Assert.That(result.error, Is.Null);
        Assert.That(result.urls.Exists(streamUrl => streamUrl.Expiry == expiry));
        Assert.That(result.urls.Exists(streamUrl => (streamUrl.Expiry < DateTimeOffset.UtcNow.AddHours(6).AddSeconds(30).ToUnixTimeSeconds()) && (streamUrl.Expiry > DateTimeOffset.UtcNow.AddHours(5).AddMinutes(59).AddSeconds(30).ToUnixTimeSeconds())));
    }
}