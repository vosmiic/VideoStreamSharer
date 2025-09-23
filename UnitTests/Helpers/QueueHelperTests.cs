using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using StackExchange.Redis;
using VideoStreamBackend.Helpers;
using VideoStreamBackend.Hubs;
using VideoStreamBackend.Identity;
using VideoStreamBackend.Models;
using VideoStreamBackend.Models.YtDlp;
using VideoStreamBackend.Redis;
using VideoStreamBackend.Services;
using QueueItemService = UnitTests.MockServices.QueueItemService;
using RoomService = UnitTests.MockServices.RoomService;

namespace UnitTests.Helpers;

[TestFixture]
public class QueueHelperTests {
    
    [TestCase("https://example.com?v=123&list=234&start_radio=1&pp=345", "https://example.com/?v=123")]
    public void GetYouTubeUrlTests(string url, string expected) {
        var result = QueueHelper.GetYouTubeUrl(new Uri(url));
        Assert.That(result.ToString(), Is.EqualTo(expected));
    }

    [Test]
    public async Task RemoveCurrentVideoFromRoomTests() {
        IQueueItemService queueItemService = new QueueItemService();
        IRoomService roomService = new RoomService();
        Mock<IDatabase> redis = new Mock<IDatabase>();
        Mock<IHubCallerClients>  clients = new Mock<IHubCallerClients>();
        var roomId = Guid.NewGuid();

        QueueItem toBeDeleted = new QueueItem {
            Order = 0,
            RoomId = roomId
        };
        
        
        Room room = new Room {
            Id = roomId,
            Queue = new List<QueueItem> {
                toBeDeleted, 
                new QueueItem {
                    Order = 1,
                    RoomId = roomId
                },
                new QueueItem {
                    Order = 2,
                    RoomId = roomId
                }
            }
        };
        
        queueItemService.BulkAddOrUpdate(room.Queue);

        redis.Setup(redis => redis.HashDelete(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>())).Returns(true);
        redis.Setup(redis => redis.HashGetAsync(roomId.ToString(), RedisKeys.RoomCurrentVideoField(), CommandFlags.None)).Returns(Task.FromResult(new RedisValue(JsonSerializer.Serialize(new StreamUrl {
            Url = "htttps://example.com",
            StreamType = StreamType.Video
        }))));
        redis.Setup(redis => redis.HashGetAsync(roomId.ToString(), RedisKeys.RoomCurrentAudioField(), CommandFlags.None)).Returns(Task.FromResult(new RedisValue(JsonSerializer.Serialize(new StreamUrl {
            Url = "htttps://example.com",
            StreamType = StreamType.Audio
        }))));
        clients.Setup(client => client.Group(It.IsAny<string>()).SendCoreAsync(PrimaryHub.VideoFinishedMethod, It.IsAny<object[]>(), CancellationToken.None)).Returns(Task.CompletedTask);
        
        await QueueHelper.RemoveRoomVideo(queueItemService, redis.Object, roomService, clients.Object, room, toBeDeleted);
        
        Assert.That(room.Queue.Count, Is.EqualTo(2));
        Assert.That(room.Queue.MaxBy(queue => queue.Order)?.Order, Is.EqualTo(1));
    }
}