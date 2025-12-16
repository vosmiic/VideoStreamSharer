using System.Text.Json;
using StackExchange.Redis;
using VideoStreamBackend.Interfaces;
using VideoStreamBackend.Models;
using VideoStreamBackend.Models.ApiModels;
using VideoStreamBackend.Models.PlayableType;
using VideoStreamBackend.Models.YtDlp;
using VideoStreamBackend.Redis;
using VideoStreamBackend.Services;

namespace VideoStreamBackend.Helpers;

public class RoomHelper {
    /// <summary>
    /// Retrieve the stream URLs from either redis or generate them.
    /// </summary>
    /// <param name="redis">Instance of <see cref="IDatabase"/>.</param>
    /// <param name="request">Request from user.</param>
    /// <param name="room">Instance of <see cref="Room"/> to get the stream URLs of.</param>
    /// <returns>List of <see cref="StreamUrl"/>s.</returns>
    internal static async Task<List<StreamUrl>?> GetStreamUrls(IDatabase redis, HttpRequest request, Room room) {
        QueueItem? queueItem = room.CurrentVideo();
        if (queueItem == null) return new List<StreamUrl>();
        if (queueItem is UploadedMedia uploadedMedia) {
            return [
                new StreamUrl {
                    StreamType = StreamType.VideoAndAudio,
                    Url = GetVideoFileUrl(request, uploadedMedia.Path),
                }
            ];
        }
        
        List<StreamUrl> streamUrls = new List<StreamUrl>();
        YtDlpHelper ytDlpHelper = new YtDlpHelper(new CliWrapper());
        if (redis.HashExists(RedisKeys.RoomStreamsKey(room.Id), queueItem.Id.ToString())) {
            var redisStreamUrls = redis.HashGet(RedisKeys.RoomStreamsKey(room.Id), queueItem.Id.ToString());
            if (redisStreamUrls.HasValue) {
                IEnumerable<StreamUrl>? videoUrl;
                try {
                    videoUrl = JsonSerializer.Deserialize<IEnumerable<StreamUrl>>(redisStreamUrls.ToString());
                } catch (Exception) {
                    videoUrl = null;
                }

                if (videoUrl != null) {
                    streamUrls = videoUrl.ToList();
                }
            }
        }
        
        if (streamUrls.Count == 0 && queueItem is YouTubeVideo youtubeVideo && youtubeVideo.VideoUrl != null) {
            // store the video in redis for others
            var streams = await QueueHelper.GetStreams( ytDlpHelper, youtubeVideo.VideoUrl);
            if (streams.error != null || streams.urls == null) return null;
            QueueHelper.StoreStreams(redis, streams.urls, room.Id, youtubeVideo.Id);

            streamUrls = streams.urls.ToList();
        }

        return streamUrls;
    }

    public static async Task ResetRoomCurrentVideo(IDatabase redis, IRoomService roomService, Room room, QueueItem currentVideo) {
        redis.HashDelete(room.StringifiedId, RedisKeys.RoomUpdateTimeCounterField());
        redis.HashDelete(room.StringifiedId, RedisKeys.RoomCurrentTimeField());
        room.CurrentTime = 0;
        await roomService.SaveChanges();
        redis.HashDelete(RedisKeys.RoomStreamsKey(room.Id), currentVideo.Id.ToString());
        redis.HashDelete(room.StringifiedId, RedisKeys.RoomCurrentVideoField());
        redis.HashDelete(room.StringifiedId, RedisKeys.RoomCurrentAudioField());
    }

    /// <summary>
    /// Get the <see cref="Room"/>'s related queue in <see cref="QueueItemApiModel"/> format.
    /// </summary>
    /// <param name="room"><see cref="Room"/> to get the queue of.</param>
    /// <param name="request"><see cref="HttpRequest"/> made to retreive this data.</param>
    /// <returns>Enumerable of queue items.</returns>
    public static IEnumerable<QueueItemApiModel> GetQueueModel(Room room, HttpRequest request) =>
        room.Queue.Select(q => new QueueItemApiModel {
            Id = q.Id,
            Title = q.Title,
            ThumbnailLocation = q is UploadedMedia ? GetVideoFileUrl(request, q.ThumbnailLocation) : q.ThumbnailLocation,
            Order = q.Order,
            Type = q.GetType().Name
        });

    /// <summary>
    /// Generate a new file path for a video file.
    /// </summary>
    /// <param name="roomDirectory">Directory of the room.</param>
    /// <param name="fileName">Name of video file.</param>
    /// <returns>New video file name.</returns>
    public static string GetVideoFilePath(string roomDirectory, string fileName) =>
        Path.Combine(roomDirectory, $"{Guid.NewGuid().ToString().Replace("-", "")}{Path.GetExtension(fileName)}");

    /// <summary>
    /// Get the supplied <see cref="Room"/>'s directory to store associated files.
    /// </summary>
    /// <param name="configuration">Instance of <see cref="IConfiguration"/></param>
    /// <param name="roomId">ID of the room to the directory of.</param>
    /// <returns>Path of the <see cref="Room"/>'s directory if successful, true if successful, error if failure.</returns>
    public static (string? path, bool success, string? error) GetRoomDirectory(IConfiguration configuration, Guid roomId) {
        string? rootUploadDirectoryConfig = configuration["videoUploadStorageFolder"];
        if (rootUploadDirectoryConfig == null) return (null, false, "Video upload storage folder configuration entry missing");
        DirectoryInfo rootUploadDirectory = new DirectoryInfo(rootUploadDirectoryConfig);
        if (!rootUploadDirectory.Exists) return (null, false, "Video upload storage folder does not exist");

        var directoryPermissions = FileHelper.GetDirectoryPermissions(rootUploadDirectory);
        if (!directoryPermissions.read || !directoryPermissions.write) {
            if (directoryPermissions is { read: false, write: false }) return (null, false, "No read or write permissions on video upload storage directory");
            if (!directoryPermissions.read) return (null, false, "No read permissions on video upload storage folder");
            if (!directoryPermissions.write) return (null, false, "No write permissions on video upload storage folder");
        }
        
        var roomDirectory = Path.Combine(rootUploadDirectoryConfig, roomId.ToString());
        Directory.CreateDirectory(roomDirectory);
        return (roomDirectory, true, null);
    }

    /// <summary>
    /// Get URL of video file.
    /// </summary>
    /// <param name="request"><see cref="HttpRequest"/> made to retrieve this data.</param>
    /// <param name="videoPath">Path of the video file to get the URL of.</param>
    /// <returns>URL of video file.</returns>
    public static string GetVideoFileUrl(HttpRequest request, string videoPath) => $"{request.Scheme}://{request.Host}{request.PathBase}/files{(videoPath.StartsWith('/') ? videoPath : $"/{videoPath}")}";
}