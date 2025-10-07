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
        if (queueItem is UploadedMedia uploadedMedia) {
            return [
                new StreamUrl {
                    StreamType = StreamType.VideoAndAudio,
                    Url = GetVideoFileUrl(request, uploadedMedia.Path),
                }
            ];
        }
        
        RedisValue? videoUrlFromRedis = await redis.HashGetAsync(room.StringifiedId, RedisKeys.RoomCurrentVideoField());
        RedisValue? audioUrlFromRedis = await redis.HashGetAsync(room.StringifiedId, RedisKeys.RoomCurrentAudioField());

        List<StreamUrl> streamUrls = new List<StreamUrl>();
        if (videoUrlFromRedis is { HasValue: true } || audioUrlFromRedis is { HasValue: true }) {
            // retrieve the urls
            if (videoUrlFromRedis.HasValue) {
                streamUrls.Add(JsonSerializer.Deserialize<StreamUrl>(videoUrlFromRedis.Value.ToString()));
            }

            if (audioUrlFromRedis.HasValue) {
                streamUrls.Add(JsonSerializer.Deserialize<StreamUrl>(audioUrlFromRedis.Value.ToString()));
            }
        } else if (queueItem != null) {
            // store the video in redis for others
            YtDlpHelper ytDlpHelper = new YtDlpHelper(new CliWrapper());
            var result = await ytDlpHelper.GetVideoUrls(queueItem);
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

    public static IEnumerable<QueueItemApiModel> GetQueueModel(Room room) =>
        room.Queue.Select(q => new QueueItemApiModel {
            Id = q.Id,
            Title = q.Title,
            ThumbnailLocation = q.ThumbnailLocation,
            Order = q.Order,
            Type = q.GetType().Name
        });

    public static (bool success, string? error, string? filePath) GetVideoFilePath(IConfiguration configuration, Guid roomId, string fileName) {
        string? rootUploadDirectoryConfig = configuration["videoUploadStorageFolder"];
        if (rootUploadDirectoryConfig == null) return (false, "Video upload storage folder configuration entry missing", null);
        DirectoryInfo rootUploadDirectory = new DirectoryInfo(rootUploadDirectoryConfig);
        if (!rootUploadDirectory.Exists) return (false, "Video upload storage folder does not exist", null);

        try {
            rootUploadDirectory.EnumerateFiles();
        } catch (SecurityException) {
            return (false, "Cannot read video upload storage folder", null);
        }

        try {
            string temporaryFile = Path.Combine(rootUploadDirectoryConfig, $"temp_write_test_{Guid.NewGuid()}.tmp");
            File.WriteAllText(temporaryFile, string.Empty);
            File.Delete(temporaryFile);
        } catch (UnauthorizedAccessException) {
            return (false, "Cannot write video upload storage folder", null);
        } catch (SecurityException) {
            return (false, "Cannot write video upload storage folder", null);
        }
        
        string extension = Path.GetExtension(fileName);
        var roomDirectory = Path.Combine(rootUploadDirectoryConfig, roomId.ToString());
        Directory.CreateDirectory(roomDirectory);
        
        return (true, null, Path.Combine(roomDirectory, $"{Guid.NewGuid().ToString().Replace("-", "")}{extension}"));
    }

    private static string GetVideoFileUrl(HttpRequest request, string videoPath) => $"{request.Scheme}://{request.Host}{request.PathBase}/files{(videoPath.StartsWith('/') ? videoPath : $"/{videoPath}")}";
}