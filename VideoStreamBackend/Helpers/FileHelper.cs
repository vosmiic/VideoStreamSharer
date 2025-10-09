using System.Security;

namespace VideoStreamBackend.Helpers;

public class FileHelper {
    /// <summary>
    /// Get permissions of a directory.
    /// </summary>
    /// <param name="directory">Directory to get the permissions of.</param>
    /// <returns>True if read permissions, true if write permissions.</returns>
    public static (bool read, bool write) GetDirectoryPermissions(DirectoryInfo directory) {
        bool read = false;
        try {
            directory.EnumerateFiles();
            read = true;
        } catch (SecurityException) {
            // Cannot read
        }

        bool write = false;
        try {
            string temporaryFile = Path.Combine(directory.FullName, $"temp_write_test_{Guid.NewGuid()}.tmp");
            File.WriteAllText(temporaryFile, string.Empty);
            File.Delete(temporaryFile);
            write = true;
        } catch (UnauthorizedAccessException) {
            // Cannot write
        } catch (SecurityException) {
            // Cannot write
        }
        
        return (read, write);
    }
}