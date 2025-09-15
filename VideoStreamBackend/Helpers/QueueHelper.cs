using System.Web;

namespace VideoStreamBackend.Helpers;

public class QueueHelper {
    internal static Uri GetYouTubeUrl(Uri originalUri) {
        var parameters = HttpUtility.ParseQueryString(originalUri.Query);
        return new Uri($"{originalUri.GetLeftPart(UriPartial.Path)}?v={parameters["v"]}");
    }
}