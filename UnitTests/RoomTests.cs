using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Moq;
using StackExchange.Redis;
using VideoStreamBackend.Helpers;
using VideoStreamBackend.Models;
using VideoStreamBackend.Models.YtDlp;
using VideoStreamBackend.Redis;

namespace UnitTests;

[TestFixture]
public class RoomTests {
    private Mock<IDatabase> redisMock;

    [SetUp]
    public void Setup() {
        redisMock = new Mock<IDatabase>();
    }

    [Test]
    public async Task GetStreamUrls_FromRedis() {
        redisMock.Setup(redis => redis.HashGetAsync(It.IsAny<RedisKey>(), RedisKeys.RoomCurrentVideoField(), It.IsAny<CommandFlags>())).Returns(Task.FromResult(new RedisValue(JsonSerializer.Serialize(new StreamUrl {
            Url = "https://fakeurl.com",
            StreamType = StreamType.Video
        }))));
        
        redisMock.Setup(redis => redis.HashGetAsync(It.IsAny<RedisKey>(), RedisKeys.RoomCurrentAudioField(), It.IsAny<CommandFlags>())).Returns(Task.FromResult(new RedisValue(JsonSerializer.Serialize(new StreamUrl {
            Url = "https://fakeurl.com",
            StreamType = StreamType.Audio
        }))));

        Room room = new Room {
            Id = Guid.Empty
        };

        List<StreamUrl>? streamUrls = await RoomHelper.GetStreamUrls(redisMock.Object, new Mock<HttpRequest>().Object, room);
        
        Assert.That(streamUrls, Is.Not.Null);
        Assert.That(streamUrls, Is.Not.Empty);
        Assert.That(streamUrls.FirstOrDefault(streamUrl => streamUrl.StreamType == StreamType.Video), Is.Not.Null);
        Assert.That(streamUrls.FirstOrDefault(streamUrl => streamUrl.StreamType == StreamType.Audio), Is.Not.Null);
        Assert.That(streamUrls.Count, Is.EqualTo(2));
    }
}