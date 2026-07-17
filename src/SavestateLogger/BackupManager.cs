using System;
using System.IO;
using System.Text;

namespace SavestateLogger
{
    public class BackupManager
    {
        /// <summary>
        /// Saves the extracted text state of a process to the specified backup directory.
        /// </summary>
        /// <param name="baseDirectory">The root directory for saving states.</param>
        /// <param name="processId">The OS process identifier.</param>
        /// <param name="windowTitle">The title of the window (used for naming the backup file).</param>
        /// <param name="content">The text content to save.</param>
        /// <returns>The full path of the saved backup file.</returns>
        public static string SaveState(string baseDirectory, int processId, string windowTitle, string content)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
                throw new ArgumentException("Base directory path cannot be null or empty.", nameof(baseDirectory));

            // Ensure backup directory exists
            if (!Directory.Exists(baseDirectory))
            {
                Directory.CreateDirectory(baseDirectory);
            }

            // Sanitize window title to prevent invalid filename characters
            string sanitizedTitle = SanitizeFileName(windowTitle);

            // Create timestamp (format: yyyyMMdd_HHmmss)
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Build unique, clean filename
            string fileName = $"{timestamp}_{processId}_{sanitizedTitle}.txt";
            string fullPath = Path.Combine(baseDirectory, fileName);

            // Write content using UTF-8 encoding
            File.WriteAllText(fullPath, content ?? string.Empty, Encoding.UTF8);

            return fullPath;
        }

        /// <summary>
        /// Deletes all files in the specified directory that are older than the retention threshold.
        /// </summary>
        /// <param name="directory">The directory to sweep for old files.</param>
        /// <param name="maxAgeDays">The maximum age in days before a file is deleted.</param>
        public static void CleanupOldBackups(string directory, int maxAgeDays)
        {
            if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            {
                return;
            }

            if (maxAgeDays <= 0)
            {
                return;
            }

            DateTime cutoffTime = DateTime.Now.AddDays(-maxAgeDays);
            string[] files = Directory.GetFiles(directory, "*.txt", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(file);
                    if (fileInfo.LastWriteTime < cutoffTime)
                    {
                        fileInfo.Delete();
                    }
                }
                catch
                {
                    // Handle file lock or permission errors gracefully
                }
            }
        }

        /// <summary>
        /// Replaces any invalid file name characters with an underscore.
        /// </summary>
        private static string SanitizeFileName(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "Untitled";
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (Array.IndexOf(invalidChars, c) >= 0)
                {
                    sb.Append('_');
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}