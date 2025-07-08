using System.Text;
using System.Text.Json;
using CliWrap;
using CliWrap.Buffered;
using VideoStreamBackend.Models.YtDlp;

namespace VideoStreamBackend.Helpers;

public class YtDlpHelper {
    public YtDlpHelper() {
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

    public async Task<(VideoInfo? videoInfo, bool success, string? error)> GetVideoInfo(Uri uri) {
        StringBuilder standardOutput = new StringBuilder();
        StringBuilder errorOutput = new StringBuilder();
        var argument = $"-q -O \"%(.{{{nameof(VideoInfo.Title).ToLower()}}})j\" {uri}";
        var result = await Cli.Wrap("yt-dlp")
            .WithArguments(argument)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(standardOutput))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(errorOutput))
            .ExecuteBufferedAsync();
        
        if (!result.IsSuccess) return (null, result.IsSuccess, errorOutput.ToString());
        
        VideoInfo? videoInfo = JsonSerializer.Deserialize<VideoInfo>(standardOutput.ToString());
        if (videoInfo == null) return (null, false, "Could not parse video info");

        return (videoInfo, true, null);
    }

    public async Task<(List<StreamUrl>? urls, bool success, string? error)> GetVideoUrls(Uri uri) {
        StringBuilder standardOutput = new StringBuilder();
        StringBuilder errorOutput = new StringBuilder();
        
        var argument = $"-q -g \"{uri}\" -f \"bv+ba\"";
        var result = await Cli.Wrap("yt-dlp")
            .WithArguments(argument)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(standardOutput))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(errorOutput))
            .ExecuteBufferedAsync();
        
        if (!result.IsSuccess) return (null, result.IsSuccess, errorOutput.ToString());
        var urls = standardOutput.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        List<StreamUrl> streamUrls = new List<StreamUrl>();
        streamUrls.Add(new StreamUrl {
            Url = urls[0],
            StreamType = StreamType.Video
        });
        streamUrls.Add(new StreamUrl {
            Url = urls[1],
            StreamType = StreamType.Audio
        });
        
        return (streamUrls, result.IsSuccess, null);
    }
}