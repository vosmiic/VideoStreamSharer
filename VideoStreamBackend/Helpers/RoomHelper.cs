using System.Text;
using System.Text.Json;
using CliWrap;
using StackExchange.Redis;
using VideoStreamBackend.Interfaces;
using VideoStreamBackend.Models;
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
        RedisValue? videoUrlFromRedis = await redis.HashGetAsync(RedisKeys.RoomKey(room.Id), RedisKeys.RoomCurrentVideoField());
        RedisValue? audioUrlFromRedis = await redis.HashGetAsync(RedisKeys.RoomKey(room.Id), RedisKeys.RoomCurrentAudioField());
        
        List<StreamUrl> streamUrls = new List<StreamUrl>();
        if (videoUrlFromRedis is { HasValue: true } || audioUrlFromRedis is { HasValue: true }) {
            // retrieve the urls
            if (videoUrlFromRedis.HasValue) {
                streamUrls.Add(JsonSerializer.Deserialize<StreamUrl>(videoUrlFromRedis.Value.ToString()));
            }

            if (audioUrlFromRedis.HasValue) {
                streamUrls.Add(JsonSerializer.Deserialize<StreamUrl>(audioUrlFromRedis.Value.ToString()));
            }
        } else {
            // store the video in redis for others
            YtDlpHelper ytDlpHelper = new YtDlpHelper(new CliWrapper());
            StringBuilder standardOutput = new StringBuilder();
            StringBuilder errorOutput = new StringBuilder();
            var result = await ytDlpHelper.GetVideoUrls(room.CurrentVideo() is YouTubeVideo youTubeVideo ? youTubeVideo.VideoUrl : null, standardOutput, errorOutput);
            if (!result.success || result.urls == null) {
                return streamUrls;
            }

            foreach (StreamUrl streamUrl in result.urls) {
                if (streamUrl.StreamType == StreamType.Video) {
                    redis.HashSet(room.Id.ToString(), RedisKeys.RoomCurrentVideoField(), JsonSerializer.Serialize(streamUrl));
                    await redis.HashFieldExpireAsync(room.Id.ToString(), [ RedisKeys.RoomCurrentVideoField() ], DateTimeOffset.FromUnixTimeSeconds(streamUrl.Expiry).UtcDateTime);
                    break;
                }

                if (streamUrl.StreamType == StreamType.Audio) {
                    redis.HashSet(room.Id.ToString(), RedisKeys.RoomCurrentAudioField(), JsonSerializer.Serialize(streamUrl));
                    await redis.HashFieldExpireAsync(room.Id.ToString(), [ RedisKeys.RoomCurrentAudioField() ], DateTimeOffset.FromUnixTimeSeconds(streamUrl.Expiry).UtcDateTime);
                    break;
                }
            }
            
            streamUrls = result.urls;
        }

        return streamUrls;
    }
}