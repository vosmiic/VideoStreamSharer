using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Moq;
using StackExchange.Redis;
using VideoStreamBackend.Helpers;
using VideoStreamBackend.Hubs;
using VideoStreamBackend.Models;
using VideoStreamBackend.Models.YtDlp;
using VideoStreamBackend.Redis;
using VideoStreamBackend.Services;
using QueueItemService = UnitTests.MockServices.QueueItemService;
using RoomService = UnitTests.MockServices.RoomService;

namespace UnitTests.Helpers;

[TestFixture]
public class QueueHelperTests {
    IQueueItemService _queueItemService;
    IRoomService _roomService;
    Mock<IDatabase> _redis;
    Mock<IHubCallerClients> _clients;
        
    [SetUp]
    public void SetUp() {
        _queueItemService = new QueueItemService();
        _roomService = new RoomService();
        _redis = new Mock<IDatabase>();
        _clients = new Mock<IHubCallerClients>();
    }
        
    
    [TestCase("https://example.com?v=123&list=234&start_radio=1&pp=345", "https://example.com/?v=123")]
    public void GetYouTubeUrlTests(string url, string expected) {
        var result = QueueHelper.GetYouTubeUrl(new Uri(url));
        Assert.That(result.ToString(), Is.EqualTo(expected));
    }

    private void RemoveVideoMockSetups(string roomId) {
        _redis.Setup(redis => redis.HashDelete(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>())).Returns(true);
        _redis.Setup(redis => redis.HashGetAsync(roomId, RedisKeys.RoomCurrentVideoField(), CommandFlags.None)).Returns(Task.FromResult(new RedisValue(JsonSerializer.Serialize(new StreamUrl {
            Url = "htttps://example.com",
            StreamType = StreamType.Video
        }))));
        _redis.Setup(redis => redis.HashGetAsync(roomId, RedisKeys.RoomCurrentAudioField(), CommandFlags.None)).Returns(Task.FromResult(new RedisValue(JsonSerializer.Serialize(new StreamUrl {
            Url = "htttps://example.com",
            StreamType = StreamType.Audio
        }))));
        _clients.Setup(client => client.Group(It.IsAny<string>()).SendCoreAsync(PrimaryHub.VideoFinishedMethod, It.IsAny<object[]>(), CancellationToken.None)).Returns(Task.CompletedTask);
    }

    [Test]
    public async Task RemoveCurrentVideoFromRoomTests() {
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
        
        _queueItemService.BulkAddOrUpdate(room.Queue);
        RemoveVideoMockSetups(room.StringifiedId);
        await QueueHelper.RemoveRoomVideo(_queueItemService, _redis.Object, _roomService, _clients.Object, new Mock<HttpRequest>().Object, room, toBeDeleted);
        
        Assert.That(room.Queue.Count, Is.EqualTo(2));
        Assert.That(room.Queue.MaxBy(queue => queue.Order)?.Order, Is.EqualTo(1));
    }

    [Test]
    public async Task RemoveVideoFromRoomTests() {
        var roomId = Guid.NewGuid();

        QueueItem toBeDeleted = new QueueItem {
            Order = 1,
            RoomId = roomId
        };
        
        
        Room room = new Room {
            Id = roomId,
            Queue = new List<QueueItem> {
                toBeDeleted, 
                new QueueItem {
                    Order = 0,
                    RoomId = roomId
                },
                new QueueItem {
                    Order = 2,
                    RoomId = roomId
                }
            }
        };
        
        _queueItemService.BulkAddOrUpdate(room.Queue);
        RemoveVideoMockSetups(room.StringifiedId);
        await QueueHelper.RemoveRoomVideo(_queueItemService, _redis.Object, _roomService, _clients.Object, new Mock<HttpRequest>().Object, room, toBeDeleted);
        
        Assert.That(room.Queue.Count, Is.EqualTo(2));
        Assert.That(room.Queue.FirstOrDefault(item => item.Order == 0), Is.Not.Null);
        Assert.That(room.Queue.FirstOrDefault(item => item.Order == 1), Is.Not.Null);
    }
}