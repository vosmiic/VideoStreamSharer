using System.Collections.Specialized;
using System.Text;
using System.Text.Json;
using System.Web;
using CliWrap;
using CliWrap.Buffered;
using VideoStreamBackend.Interfaces;
using VideoStreamBackend.Models.YtDlp;

namespace VideoStreamBackend.Helpers;

public class YtDlpHelper {
    private readonly ICliWrapper _cliWrapper;
    private readonly string YtDlpFilePath = "yt-dlp"; // todo this should be changed to be user customizable
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
        var argument = $"-q -O \"%(.{{{nameof(VideoInfo.Title).ToLower()}}})j\" {uri}";
        var result = await _cliWrapper.ExecuteBufferedAsync(YtDlpFilePath, argument, PipeTarget.ToStringBuilder(standardOutput), PipeTarget.ToStringBuilder(errorOutput));
        if (!result.IsSuccess) return (null, result.IsSuccess, errorOutput.ToString());
        
        VideoInfo? videoInfo = JsonSerializer.Deserialize<VideoInfo>(standardOutput.ToString());
        if (videoInfo == null) return (null, false, "Could not parse video info");

        return (videoInfo, true, null);
    }

    public async Task<(List<StreamUrl>? urls, bool success, string? error)> GetVideoUrls(Uri uri, StringBuilder standardOutput, StringBuilder errorOutput) {
        var argument = $"-q -g \"{uri}\" -f \"bv+ba\"";
        var result = await _cliWrapper.ExecuteBufferedAsync(YtDlpFilePath, argument, PipeTarget.ToStringBuilder(standardOutput), PipeTarget.ToStringBuilder(errorOutput));
        
        if (!result.IsSuccess) return (null, result.IsSuccess, errorOutput.ToString());
        var urls = standardOutput.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        List<StreamUrl> streamUrls = new List<StreamUrl>();
        foreach (string url in urls) {
            Uri streamUri = new Uri(url);
            NameValueCollection parameters = HttpUtility.ParseQueryString(streamUri.Query);
            string? expiry = parameters.Get("expire");
            streamUrls.Add(new StreamUrl {
                Url = url,
                StreamType = Array.IndexOf(urls, url) == 0 ? StreamType.Video : StreamType.Audio,
                Expiry = expiry != null && long.TryParse(expiry, out long unixTime) ? unixTime : DateTimeOffset.UtcNow.AddHours(6).ToUnixTimeSeconds()
            });
        }
        
        return (streamUrls, result.IsSuccess, null);
    }
}