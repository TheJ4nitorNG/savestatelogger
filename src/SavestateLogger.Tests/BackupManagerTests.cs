using System;
using System.IO;
using Xunit;

namespace SavestateLogger.Tests
{
    public class BackupManagerTests : IDisposable
    {
        private readonly string _tempDir;

        public BackupManagerTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SavestateLogger_Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [Fact]
        public void TestSaveStateCreatesFileWithSanitizedName()
        {
            // Arrange
            int processId = 9999;
            string rawWindowTitle = "My File *Unsaved*? - Notepad"; // Contains invalid chars: *, ?
            string expectedSanitizedTitle = "My File _Unsaved__ - Notepad"; 
            string content = "Hello world! This is a backup of Notepad content.";

            // Act
            string savedPath = BackupManager.SaveState(_tempDir, processId, rawWindowTitle, content);

            // Assert
            Assert.True(File.Exists(savedPath));
            
            // Check that the content is identical
            string readContent = File.ReadAllText(savedPath);
            Assert.Equal(content, readContent);

            // Check that the filename contains the processId and sanitized title
            string fileName = Path.GetFileName(savedPath);
            Assert.Contains(processId.ToString(), fileName);
            Assert.Contains(expectedSanitizedTitle, fileName);
            Assert.EndsWith(".txt", fileName);
        }

        [Fact]
        public void TestCleanupOldBackupsDeletesOlderFiles()
        {
            // Arrange
            string oldFile = Path.Combine(_tempDir, "20260701_120000_1111_OldBackup.txt");
            string recentFile = Path.Combine(_tempDir, "20260715_120000_2222_RecentBackup.txt");
            string currentFile = Path.Combine(_tempDir, "20260717_120000_3333_CurrentBackup.txt");

            File.WriteAllText(oldFile, "Old content");
            File.WriteAllText(recentFile, "Recent content");
            File.WriteAllText(currentFile, "Current content");

            // Manually set the LastWriteTime of the files to represent age
            File.SetLastWriteTime(oldFile, DateTime.Now.AddDays(-10));  // 10 days old (should delete)
            File.SetLastWriteTime(recentFile, DateTime.Now.AddDays(-5)); // 5 days old (should keep)
            File.SetLastWriteTime(currentFile, DateTime.Now);            // 0 days old (should keep)

            // Act
            BackupManager.CleanupOldBackups(_tempDir, 7);

            // Assert
            Assert.False(File.Exists(oldFile));
            Assert.True(File.Exists(recentFile));
            Assert.True(File.Exists(currentFile));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try
                {
                    Directory.Delete(_tempDir, true);
                }
                catch
                {
                    // Ignore cleanup errors in tests
                }
            }
        }
    }
}