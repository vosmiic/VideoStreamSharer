using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using VideoStreamBackend.Hubs;
using VideoStreamBackend.Models;
using VideoStreamBackend.Models.ApiModels;
using VideoStreamBackend.Models.YtDlp;
using VideoStreamBackend.Redis;
using VideoStreamBackend.Services;

namespace VideoStreamBackend.Helpers;

public class QueueHelper {
    internal static Uri GetYouTubeUrl(Uri originalUri) {
        var parameters = HttpUtility.ParseQueryString(originalUri.Query);
        return new Uri($"{originalUri.GetLeftPart(UriPartial.Path)}?v={parameters["v"]}");
    }

    internal static async Task RemoveRoomVideo(IQueueItemService queueItemService, IDatabase redis, IRoomService roomService, IHubCallerClients clients, HttpRequest request, Room room, QueueItem videoToDelete) {
        room.Queue.Remove(videoToDelete);
        
        // re-order the queue
        foreach (QueueItem queueItem in room.Queue.Where(item => item.Order > videoToDelete.Order)) {
            queueItem.Order--;
        }

        await queueItemService.SaveChanges();

        if (videoToDelete.Order == 0) {
            await RoomHelper.ResetRoomCurrentVideo(redis, roomService, room, videoToDelete);
            var result = await RoomHelper.GetStreamUrls(redis, request, room);
            if (result == null) return;
            await clients.Group(room.StringifiedId).SendAsync(PrimaryHub.VideoFinishedMethod, new GetRoomResponse {
                Room = new RoomApiModel {
                    StreamUrls = result,
                    Queue = RoomHelper.GetQueueModel(room, request)
                }
            });
        } else {
            await clients.Group(room.StringifiedId).SendAsync(PrimaryHub.DeleteQueueMethod, videoToDelete.Id);
            await clients.Group(room.StringifiedId).SendAsync(PrimaryHub.LoadVideoMethod, await RoomHelper.GetStreamUrls(redis, request, room));
        }
    }

    /// <summary>
    /// Generate a thumbnail for the provided video file.
    /// </summary>
    /// <param name="ytDlpHelper">Instance of <see cref="YtDlpHelper"/>.</param>
    /// <param name="configuration">Instance of <see cref="IConfiguration"/>.</param>
    /// <param name="roomId">ID of the room that the related <see cref="QueueItem"/> belongs to.</param>
    /// <param name="videoFilePath">File path of the video to generate the thumbnail for.</param>
    /// <param name="fileName">File name of the generated image.</param>
    /// <returns>Path of thumbnail if successful, true if success, error if failure.</returns>
    internal static async Task<(string? thumbnailPath, bool success, string? error)> GenerateThumbnail(YtDlpHelper ytDlpHelper, IConfiguration configuration, Guid roomId, string videoFilePath, string fileName) {
        FileInfo fileInfo;
        try {
            fileInfo = new FileInfo(videoFilePath); 
        } catch (Exception e) {
            Console.WriteLine(e); //todo log this properly
            return (null, false, $"Error: {e.Message}");
        }
        
        var videoDurationTask = await ytDlpHelper.GetVideoDuration(fileInfo.FullName);
        if (videoDurationTask is not { success: true, duration: not null }) return (null, false, $"Could not get video duration: {videoDurationTask.error}");
        Random random = new Random(DateTime.Now.Millisecond);
        int randomScreenshotTime = random.Next((int)videoDurationTask.duration.Value);
        DateTimeOffset convertedScreenshotTime = DateTimeOffset.FromUnixTimeSeconds(randomScreenshotTime);
        var roomDirectoryTask = RoomHelper.GetRoomDirectory(configuration, roomId);
        if (!roomDirectoryTask.success || roomDirectoryTask.path == null) return (null, false, $"Error: {roomDirectoryTask.error}");
        string thumbnailPath = Path.Combine(roomDirectoryTask.path, $"{fileName}.jpg");
        var generatedThumbnail = await ytDlpHelper.GenerateThumbnail(fileInfo.FullName, convertedScreenshotTime, thumbnailPath);
        if (!generatedThumbnail.success) return (null, false, $"Could not generate thumbnail: {generatedThumbnail.error}");
        return (thumbnailPath, true, null);
    }

    internal static async Task<(List<StreamUrl>? urls, VideoInfo? videoInfo, string? error)> GetStreams( YtDlpHelper ytDlpHelper, Uri uri) {
        (VideoInfo? videoInfo, string? error) info = await RetrieveVideoInfoWithRetries(ytDlpHelper, uri);
        if (info.videoInfo == null) return (null, null, info.error);
        
        var streams = info.videoInfo.UniqueFormats().Select((format, index) => new StreamUrl() {
            Id = index, // arbitrary id to make frontend operations easier
            Resolution = format.Resolution,
            Url = format.Url,
            StreamType = format.Protocol == VideoInfo.Protocol.m3u8_native ? StreamType.VideoAndAudio : format.IsAudio ? StreamType.Audio : StreamType.Video,
            Expiry = YtDlpHelper.GetExpiry(format.Url),
            ResolutionName = format.Protocol == VideoInfo.Protocol.m3u8_native ? $"{format.Resolution} m3u8" : format.Resolution,
            Protocol = format.Protocol
        }).ToList();
        
        return (streams, info.videoInfo, null);
    }

    internal static void StoreStreams(IDatabase redis, IEnumerable<StreamUrl> streams, Guid roomId, Guid videoId) {
        redis.HashSet(RedisKeys.RoomStreamsKey(roomId), videoId.ToString(), JsonSerializer.Serialize(streams));
    }
    
    public static async Task<(VideoInfo? videoInfo, string? error)> RetrieveVideoInfoWithRetries(YtDlpHelper ytDlpHelper, Uri uri) {
        VideoInfo? info = null;
        int counter = 0;
        while (counter < 3) {
            StringBuilder standardOutput = new StringBuilder();
            StringBuilder errorOutput = new StringBuilder();
            (VideoInfo? videoInfo, bool success, string? error) videoInfo = await ytDlpHelper.GetVideoInfo(uri, standardOutput, errorOutput);
            if (!videoInfo.success || videoInfo.videoInfo == null) return (null, videoInfo.error);

            if (videoInfo.videoInfo.Formats.Length == 0) {
                // retry the request, sometimes the server provides odd results
                counter++;
            } else {
                info = videoInfo.videoInfo;
                break;
            }
        }

        return (info, null);
    }
}