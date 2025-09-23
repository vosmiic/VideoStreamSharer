using System.Web;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using VideoStreamBackend.Hubs;
using VideoStreamBackend.Models;
using VideoStreamBackend.Models.ApiModels;
using VideoStreamBackend.Services;

namespace VideoStreamBackend.Helpers;

public class QueueHelper {
    internal static Uri GetYouTubeUrl(Uri originalUri) {
        var parameters = HttpUtility.ParseQueryString(originalUri.Query);
        return new Uri($"{originalUri.GetLeftPart(UriPartial.Path)}?v={parameters["v"]}");
    }

    internal static async Task RemoveRoomVideo(IQueueItemService queueItemService, IDatabase redis, IRoomService roomService, IHubCallerClients clients, Room room, QueueItem videoToDelete) {
        room.Queue.Remove(videoToDelete);
        
        // re-order the queue
        foreach (QueueItem queueItem in room.Queue.Where(item => item.Order > videoToDelete.Order)) {
            queueItem.Order--;
        }

        await queueItemService.SaveChanges();

        if (room.CurrentVideo() == videoToDelete) {
            await RoomHelper.ResetRoomCurrentVideo(redis, roomService, room, room.StringifiedId);
            var result = await RoomHelper.GetStreamUrls(redis, room);
            if (result == null) return;
            await clients.Group(room.StringifiedId).SendAsync(PrimaryHub.VideoFinishedMethod, new GetRoomResponse {
                Room = new RoomApiModel {
                    StreamUrls = result,
                    Queue = RoomHelper.GetQueueModel(room)
                }
            });
        } else {
            await clients.Group(room.StringifiedId).SendAsync(PrimaryHub.DeleteQueueMethod, videoToDelete.Id);
        }
    }
}