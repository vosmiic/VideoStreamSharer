using System.Security;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json;
using CliWrap;
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
        if (redis.HashExists(room.StringifiedId, RedisKeys.RoomCurrentVideoField())) {
            RedisValue? videoUrlFromRedis = await redis.HashGetAsync(room.StringifiedId, RedisKeys.RoomCurrentVideoField());
            if (videoUrlFromRedis.HasValue) {
                StreamUrl? videoUrl;
                try {
                    videoUrl = JsonSerializer.Deserialize<StreamUrl>(videoUrlFromRedis.Value.ToString());
                } catch (Exception) {
                    videoUrl = null;
                }
                if (videoUrl != null)
                    streamUrls.Add(videoUrl);
            }
        }

        if (redis.HashExists(room.StringifiedId, RedisKeys.RoomCurrentAudioField())) {
            RedisValue? audioUrlFromRedis = await redis.HashGetAsync(room.StringifiedId, RedisKeys.RoomCurrentAudioField());

            if (audioUrlFromRedis.HasValue) {
                StreamUrl? audioUrls;
                try {
                    audioUrls = JsonSerializer.Deserialize<StreamUrl>(audioUrlFromRedis.Value.ToString());
                } catch (Exception) {
                    audioUrls = null;
                }
                
                if (audioUrls != null)
                    streamUrls.Add(audioUrls);
            }
        }
        
        if (streamUrls.Count == 0 && queueItem is YouTubeVideo youtubeVideo) {
            // store the video in redis for others
            YtDlpHelper ytDlpHelper = new YtDlpHelper(new CliWrapper());
            var result = await ytDlpHelper.GetVideoUrls(youtubeVideo);
            if (!result.success || result.urls == null) {
                return streamUrls;
            }

            foreach (StreamUrl streamUrl in result.urls) {
                if (streamUrl.StreamType == StreamType.Video) {
                    redis.HashSet(room.StringifiedId, RedisKeys.RoomCurrentVideoField(), JsonSerializer.Serialize(streamUrl));
                    await redis.HashFieldExpireAsync(room.StringifiedId, [ RedisKeys.RoomCurrentVideoField() ], DateTimeOffset.FromUnixTimeSeconds(streamUrl.Expiry).UtcDateTime);
                }

                if (streamUrl.StreamType == StreamType.Audio) {
                    redis.HashSet(room.StringifiedId, RedisKeys.RoomCurrentAudioField(), JsonSerializer.Serialize(streamUrl));
                    await redis.HashFieldExpireAsync(room.StringifiedId, [ RedisKeys.RoomCurrentAudioField() ], DateTimeOffset.FromUnixTimeSeconds(streamUrl.Expiry).UtcDateTime);
                }
            }

            streamUrls = result.urls;
        }

        return streamUrls;
    }

    public static async Task ResetRoomCurrentVideo(IDatabase redis, IRoomService roomService, Room room, string roomId) {
        redis.HashDelete(roomId, RedisKeys.RoomUpdateTimeCounterField());
        redis.HashDelete(roomId, RedisKeys.RoomCurrentTimeField());
        room.CurrentTime = 0;
        await roomService.SaveChanges();
        redis.HashDelete(roomId, RedisKeys.RoomCurrentVideoField());
        redis.HashDelete(roomId, RedisKeys.RoomCurrentAudioField());
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