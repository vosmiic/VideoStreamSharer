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

namespace VideoStreamBackend.Helpers;

public class RoomHelper {
    /// <summary>
    /// Retrieve the stream URLs from either redis or generate them.
    /// </summary>
    /// <param name="redis">Instance of <see cref="IDatabase"/>.</param>
    /// <param name="room">Instance of <see cref="Room"/> to get the stream URLs of.</param>
    /// <returns>List of <see cref="StreamUrl"/>s.</returns>
    internal static async Task<List<StreamUrl>?> GetStreamUrls(IDatabase redis, Room room) {
        string roomId = room.Id.ToString();
        RedisValue? videoUrlFromRedis = await redis.HashGetAsync(roomId, RedisKeys.RoomCurrentVideoField());
        RedisValue? audioUrlFromRedis = await redis.HashGetAsync(roomId, RedisKeys.RoomCurrentAudioField());

        List<StreamUrl> streamUrls = new List<StreamUrl>();
        QueueItem? queueItem = room.CurrentVideo();
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
                    redis.HashSet(roomId, RedisKeys.RoomCurrentVideoField(), JsonSerializer.Serialize(streamUrl));
                    await redis.HashFieldExpireAsync(roomId, [ RedisKeys.RoomCurrentVideoField() ], DateTimeOffset.FromUnixTimeSeconds(streamUrl.Expiry).UtcDateTime);
                }

                if (streamUrl.StreamType == StreamType.Audio) {
                    redis.HashSet(roomId, RedisKeys.RoomCurrentAudioField(), JsonSerializer.Serialize(streamUrl));
                    await redis.HashFieldExpireAsync(roomId, [ RedisKeys.RoomCurrentAudioField() ], DateTimeOffset.FromUnixTimeSeconds(streamUrl.Expiry).UtcDateTime);
                }
            }

            streamUrls = result.urls;
        }

        return streamUrls;
    }

    public static IEnumerable<QueueItemApiModel> GetQueueModel(Room room) =>
        room.Queue.Select(q => new QueueItemApiModel {
            Id = q.Id,
            Title = q.Title,
            ThumbnailLocation = q.ThumbnailLocation,
            Order = q.Order,
            Type = q.GetType().Name
        });
}