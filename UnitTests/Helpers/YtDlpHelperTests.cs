using System.Text;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using Moq;
using VideoStreamBackend.Helpers;
using VideoStreamBackend.Interfaces;
using VideoStreamBackend.Models;
using VideoStreamBackend.Models.PlayableType;
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
            Title = "title",
            Formats = new [] {
                new VideoInfo.StreamFormat {
                    FormatId = 1.ToString(),
                    Resolution = "1920x1080",
                    Quality = 1.0,
                    Filesize = 123456789,
                    Protocol = VideoInfo.Protocol.https
                }
            },
            Channel = new VideoInfo.VideoChannel {
                Name = "name",
                Url = "https://test.com"
            },
            Thumbnail = "https://test.com",
            Duration = 1,
            Viewcount = 1
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
    public async Task GetInfoTest2() {
        StringBuilder standardOutput = new StringBuilder();
        StringBuilder errorOutput = new StringBuilder();
        
        YtDlpHelper ytDlpHelper = new YtDlpHelper(new CliWrapper());
        Uri uri = new("https://www.youtube.com/watch?v=eCE4hYw8AAI");
        var result = await ytDlpHelper.GetVideoInfo(uri, standardOutput, errorOutput);
        Assert.That(result.success, Is.True);
        Assert.That(result.videoInfo, Is.Not.Null);
    }
}