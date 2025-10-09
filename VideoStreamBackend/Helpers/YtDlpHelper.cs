using System.Collections.Specialized;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using CliWrap;
using CliWrap.Buffered;
using VideoStreamBackend.Interfaces;
using VideoStreamBackend.Models;
using VideoStreamBackend.Models.PlayableType;
using VideoStreamBackend.Models.YtDlp;
using CommandResult = VideoStreamBackend.Interfaces.CommandResult;

namespace VideoStreamBackend.Helpers;

public class YtDlpHelper {
    private readonly ICliWrapper _cliWrapper;
    private readonly string YtDlpFilePath = "yt-dlp"; // todo this should be changed to be user customizable
    private readonly string FfprobeFilePath = "ffprobe";
    private readonly string FfmpegFilePath = "ffmpeg";
    
    public YtDlpHelper(ICliWrapper cliWrapper) {
        _cliWrapper = cliWrapper;
        // confirm yt-dlp is installed and available
        StringBuilder errorOutput = new StringBuilder();
        var result = Cli.Wrap("yt-dlp")
            .WithArguments("--version")
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(errorOutput))
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync().Task.Result;
        if (!result.IsSuccess) {
            throw new Exception($"Could not find yt-dlp: \n {errorOutput}");
        }
    }

    public async Task<(VideoInfo? videoInfo, bool success, string? error)> GetVideoInfo(Uri uri, StringBuilder standardOutput, StringBuilder errorOutput) {
        var argument = $"-q -O \"{{\\\"{nameof(VideoInfo.Title)}\\\": %(.{nameof(VideoInfo.Title).ToLower()})j, \\\"{nameof(VideoInfo.Formats)}\\\": %(.{nameof(VideoInfo.Formats).ToLower()}.:.{{format_id,{nameof(VideoInfo.StreamFormat.Resolution).ToLower()},{nameof(VideoInfo.StreamFormat.Quality).ToLower()},filesize_approx,{nameof(VideoInfo.StreamFormat.Protocol).ToLower()},vcodec,acodec,dynamic_range}})j, \\\"{nameof(VideoInfo.Thumbnail)}\\\": %(.{nameof(VideoInfo.Thumbnail).ToLower()})j, \\\"{nameof(VideoInfo.Channel)}\\\": {{\\\"{nameof(VideoInfo.VideoChannel.Name)}\\\": %(.{nameof(VideoInfo.Channel).ToLower()})j, \\\"{nameof(VideoInfo.VideoChannel.Url)}\\\": %(.channel_url)j}}, \\\"{nameof(VideoInfo.Duration)}\\\": %(.{nameof(VideoInfo.Duration).ToLower()})j, \\\"{nameof(VideoInfo.Viewcount)}\\\": %(.view_count)j}}\" {uri}";
        var result = await _cliWrapper.ExecuteBufferedAsync(YtDlpFilePath, argument, PipeTarget.ToStringBuilder(standardOutput), PipeTarget.ToStringBuilder(errorOutput));
        if (!result.IsSuccess) return (null, result.IsSuccess, errorOutput.ToString());
        
        var opts = new JsonSerializerOptions {
            PropertyNameCaseInsensitive = true
        };
        opts.Converters.Add(new JsonStringEnumConverter());
        VideoInfo? videoInfo = JsonSerializer.Deserialize<VideoInfo>(standardOutput.ToString(), opts);
        if (videoInfo == null) return (null, false, "Could not parse video info");
        
        return (videoInfo, true, null);
    }

    public async Task<(List<StreamUrl>? urls, bool success, string? error)> GetVideoUrls(QueueItem queueItem) {
        (StringBuilder, StringBuilder) videoOutput = (new StringBuilder(), new StringBuilder());
        (StringBuilder, StringBuilder) audioOutput = (new StringBuilder(), new StringBuilder());

        return await GetVideoUrls(queueItem, videoOutput, audioOutput);
    }

    public async Task<(List<StreamUrl>? urls, bool success, string? error)> GetVideoUrls(QueueItem queueItem,
        (StringBuilder standardOutput, StringBuilder errorOutput) videoOutput,
        (StringBuilder standardOutput, StringBuilder errorOutput) audioOutput) {
        YouTubeVideo? youtubeVideo = queueItem as YouTubeVideo;
        if (youtubeVideo == null) return (null, false, null); // todo handle uploaded videos
        string argument = $"-q -g \"{youtubeVideo.VideoUrl}\" -f";
        var videoTask = _cliWrapper.ExecuteBufferedAsync(YtDlpFilePath, $"{argument} \"{youtubeVideo.VideoFormatId}\"", PipeTarget.ToStringBuilder(videoOutput.standardOutput), PipeTarget.ToStringBuilder(videoOutput.errorOutput));
        var audioTask = _cliWrapper.ExecuteBufferedAsync(YtDlpFilePath, $"{argument} \"{youtubeVideo.AudioFormatId}\"", PipeTarget.ToStringBuilder(audioOutput.standardOutput), PipeTarget.ToStringBuilder(audioOutput.errorOutput));
        var result = await Task.WhenAll(videoTask, audioTask);
        if (!result.All(cr => cr.IsSuccess)) return (null, false, $"Video error: {videoOutput.errorOutput}; Audio error: {audioOutput.errorOutput}");
        
        string videoUrl = videoOutput.standardOutput.ToString();
        string audioUrl = audioOutput.standardOutput.ToString();
        List<StreamUrl> streamUrls = new List<StreamUrl> {
            new()  {
                Url = videoUrl,
                StreamType = StreamType.Video,
                Expiry = GetExpiry(videoUrl)
            },
            new()  {
                Url = audioUrl,
                StreamType = StreamType.Audio,
                Expiry = GetExpiry(audioUrl)
            }
        };
        
        return (streamUrls, true, null);
    }

    /// <summary>
    /// Get the video duration of the provided video file.
    /// </summary>
    /// <param name="path">Path of the video file to get the duration of.</param>
    /// <returns>Duration if successful, true if successful, error if failure</returns>
    public async Task<(float? duration, bool success, string? error)> GetVideoDuration(string path) =>
        await GetVideoDuration(path, new StringBuilder(), new StringBuilder());
    
    /// <summary>
    /// Get the video duration of the provided video file.
    /// </summary>
    /// <param name="path">Path of the video file to get the duration of.</param>
    /// <param name="standardOutput">Standard output <see cref="StringBuilder"/></param>
    /// <param name="errorOutput">Error output <see cref="StringBuilder"/></param>
    /// <returns>Duration if successful, true if successful, error if failure</returns>
    public async Task<(float? duration, bool success, string? error)> GetVideoDuration(string path, StringBuilder standardOutput, StringBuilder errorOutput) {
        string argument = $"-v error -select_streams v:0 -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 -i \"{path}\"";
        var task = await _cliWrapper.ExecuteBufferedAsync(FfprobeFilePath, argument,  PipeTarget.ToStringBuilder(standardOutput), PipeTarget.ToStringBuilder(errorOutput));
        if (!task.IsSuccess) return (null, false, $"Could not retrieve video duration during thumbnail generation: {errorOutput}");
        
        if (!float.TryParse(standardOutput.ToString(), out float duration)) return (null, false, $"Could not parse duration during thumbnail generation: standard output: {standardOutput}; error output: {errorOutput}");
        return (duration, true, null);
    }

    /// <summary>
    /// Generate a thumbnail based off of the provided video file.
    /// </summary>
    /// <param name="pathOfVideo">Path of the video to generate the thumbnail off of.</param>
    /// <param name="thumbnailScreenshotTime">Point in the video to generate the thumbnail at.</param>
    /// <param name="outputPath">Path of the generated thumbnail file.</param>
    /// <returns>True if success, error if failure.</returns>
    public async Task<(bool success, string? error)> GenerateThumbnail(string pathOfVideo, DateTimeOffset thumbnailScreenshotTime, string outputPath) =>
        await GenerateThumbnail(pathOfVideo, thumbnailScreenshotTime, outputPath, new StringBuilder(), new StringBuilder());

    /// <summary>
    /// Generate a thumbnail based off of the provided video file.
    /// </summary>
    /// <param name="pathOfVideo">Path of the video to generate the thumbnail off of.</param>
    /// <param name="thumbnailScreenshotTime">Point in the video to generate the thumbnail at.</param>
    /// <param name="outputPath">Path of the generated thumbnail file.</param>
    /// <param name="standardOutput">Standard output <see cref="StringBuilder"/></param>
    /// <param name="errorOutput">Error output <see cref="StringBuilder"/></param>
    /// <returns>True if success, error if failure.</returns>
    public async Task<(bool success, string? error)> GenerateThumbnail(string pathOfVideo, DateTimeOffset thumbnailScreenshotTime, string outputPath, StringBuilder standardOutput, StringBuilder errorOutput) {
        string argument = $"-i \"{pathOfVideo}\" -ss {thumbnailScreenshotTime:T} -vframes 1 \"{outputPath}\"";
        var task = await _cliWrapper.ExecuteBufferedAsync(FfmpegFilePath, argument, PipeTarget.ToStringBuilder(standardOutput), PipeTarget.ToStringBuilder(errorOutput));
        if (!task.IsSuccess) return (false, $"Could not generate thumbnail:  {errorOutput}");

        return (true, null);
    }

    private long GetExpiry(string url) {
        Uri streamUri = new Uri(url);
        NameValueCollection parameters = HttpUtility.ParseQueryString(streamUri.Query);
        string? expiry = parameters.Get("expire");

        return expiry != null && long.TryParse(expiry, out long unixTime) ? unixTime : DateTimeOffset.UtcNow.AddHours(6).ToUnixTimeSeconds();
    }
}