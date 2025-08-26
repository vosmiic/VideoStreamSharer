using System.Text.RegularExpressions;

namespace VideoStreamBackend.Helpers;

public class StreamHelper {
    public static Guid? FindUserId(string stringToScan) {
        var match = new Regex(@"[({]?[a-fA-F0-9]{8}[-]?([a-fA-F0-9]{4}[-]?){3}[a-fA-F0-9]{12}[})]?", RegexOptions.IgnoreCase).Match(stringToScan);
        return match.Success ? Guid.Parse(match.Value) : null;
    }
}