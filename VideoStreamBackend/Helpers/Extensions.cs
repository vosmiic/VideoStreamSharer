using VideoStreamBackend.Models;
using VideoStreamBackend.Models.YtDlp;

namespace VideoStreamBackend.Helpers;

public static class Extensions {
    public static QueueItem? CurrentVideo(this Room room) => room?.Queue.MinBy(queue => queue.Order);

    /// <summary>
    /// Get the unique formats of a <see cref="VideoInfo"/>
    /// </summary>
    /// <param name="videoInfo"><see cref="VideoInfo"/> to get the unique formats of.</param>
    /// <param name="onlyVideo">True to only retrieve video, false to only retrieve audio, null to retrieve both.</param>
    /// <returns>Enumerable of <see cref="VideoInfo.StreamFormat"/></returns>
    public static IEnumerable<VideoInfo.StreamFormat> UniqueFormats(this VideoInfo videoInfo, bool? onlyVideo = null) => videoInfo.Formats
        .Where(format => onlyVideo switch {
            true => format is { Protocol: VideoInfo.Protocol.https, IsAudio: false },
            false => format is { Protocol: VideoInfo.Protocol.https, IsAudio: true },
            null => format is { Protocol: VideoInfo.Protocol.https } or { Protocol: VideoInfo.Protocol.m3u8_native, IsAudio: false }
        })
        .OrderByDescending(stream => stream.Quality)
        .ThenByDescending(stream => stream.Filesize)
        .DistinctBy(stream => (stream.Resolution, stream.Protocol));
}