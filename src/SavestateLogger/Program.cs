using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SavestateLogger
{
    internal class Program
    {
        private static readonly string RootBackupDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SavestateLogger"
        );

        private static readonly string NotepadBackupDirectory = Path.Combine(RootBackupDirectory, "NotepadBackups");
        private static readonly string TerminalBackupDirectory = Path.Combine(RootBackupDirectory, "TerminalBackups");
        private static readonly string LogFilePath = Path.Combine(RootBackupDirectory, "service.log");

        private static async Task Main(string[] args)
        {
            // Parse CLI flags first before entering daemon loop
            bool listMode = false;
            bool restoreMode = false;
            int intervalSeconds = 600; // Default 10 minutes

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--list", StringComparison.OrdinalIgnoreCase) || 
                    args[i].Equals("-l", StringComparison.OrdinalIgnoreCase))
                {
                    listMode = true;
                }
                else if (args[i].Equals("--restore", StringComparison.OrdinalIgnoreCase) || 
                         args[i].Equals("-r", StringComparison.OrdinalIgnoreCase))
                {
                    restoreMode = true;
                }
                else if ((args[i].Equals("--interval", StringComparison.OrdinalIgnoreCase) || 
                          args[i].Equals("-i", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int parsedSec) && parsedSec > 0)
                    {
                        intervalSeconds = parsedSec;
                    }
                }
            }

            if (listMode)
            {
                ListSaveStates();
                return;
            }

            if (restoreMode)
            {
                RestoreInteractive();
                return;
            }

            // Normal Daemon Loop
            Console.Title = "Savestate Logger Daemon";
            PrintHeader();

            Log($"Root backup directory: {RootBackupDirectory}");
            Log($"Notepad backup folder: {NotepadBackupDirectory}");
            Log($"Terminal backup folder: {TerminalBackupDirectory}");
            Log($"Scan interval: {intervalSeconds} seconds ({(intervalSeconds / 60.0):F1} minutes)");

            using var cts = new CancellationTokenSource();

            // Setup graceful cancellation on Ctrl+C or Ctrl+Break
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // Prevent immediate process termination
                Log("Shutdown request received. Stopping gracefully...");
                cts.Cancel();
            };

            Log("Savestate Logger is running. Press Ctrl+C to exit.");
            Log("-----------------------------------------------------------------");

            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    await PerformBackupCycle();

                    // Wait for the specified interval, or until cancelled
                    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cts.Token);
                }
            }
            catch (TaskCanceledException)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                Log($"Fatal error in service execution loop: {ex.Message}", isError: true);
            }

            Log("Service stopped. Good-bye!");
        }

        private static async Task PerformBackupCycle()
        {
            Log("Starting periodic autosave sweep...");

            // Trigger retention cleanup (keeping backups for 7 days)
            try
            {
                BackupManager.CleanupOldBackups(NotepadBackupDirectory, 7);
                BackupManager.CleanupOldBackups(TerminalBackupDirectory, 7);
            }
            catch (Exception ex)
            {
                Log($"Error during retention cleanup sweep: {ex.Message}", isError: true);
            }

            int totalSuccess = 0;
            int totalFail = 0;

            // 1. Sweep Notepad instances
            try
            {
                var notepadProcesses = NotepadScraper.GetActiveNotepadProcesses();
                foreach (var process in notepadProcesses)
                {
                    try
                    {
                        string text = NotepadScraper.ExtractText(process);
                        string windowTitle = string.IsNullOrWhiteSpace(process.MainWindowTitle) 
                            ? "Untitled Notepad" 
                            : process.MainWindowTitle;

                        string savedPath = BackupManager.SaveState(NotepadBackupDirectory, process.Id, windowTitle, text);
                        Log($"[Notepad PID {process.Id}] Backup successfully captured: \"{windowTitle}\" -> {Path.GetFileName(savedPath)}");
                        totalSuccess++;
                    }
                    catch (Exception ex)
                    {
                        Log($"[Notepad PID {process.Id}] Failed to capture state: {ex.Message}", isError: true);
                        totalFail++;
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error during Notepad backup sweep: {ex.Message}", isError: true);
            }

            // 2. Sweep Terminal instances (cmd, powershell, pwsh, WindowsTerminal)
            try
            {
                var terminalProcesses = TerminalScraper.GetActiveTerminalProcesses();
                foreach (var process in terminalProcesses)
                {
                    try
                    {
                        string text = TerminalScraper.ExtractText(process);
                        string windowTitle = string.IsNullOrWhiteSpace(process.MainWindowTitle) 
                            ? $"{process.ProcessName} Terminal" 
                            : process.MainWindowTitle;

                        string savedPath = BackupManager.SaveState(TerminalBackupDirectory, process.Id, windowTitle, text);
                        Log($"[Terminal PID {process.Id}] Backup successfully captured: \"{windowTitle}\" -> {Path.GetFileName(savedPath)}");
                        totalSuccess++;
                    }
                    catch (Exception ex)
                    {
                        Log($"[Terminal PID {process.Id}] Failed to capture state: {ex.Message}", isError: true);
                        totalFail++;
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error during Terminal backup sweep: {ex.Message}", isError: true);
            }

            Log($"Sweep complete. Total successful captures: {totalSuccess}, Failed: {totalFail}");
        }

        private static void ListSaveStates()
        {
            var files = GetSortedBackups();
            if (files.Count == 0)
            {
                Console.WriteLine("No save states found.");
                return;
            }

            Console.WriteLine("\n================ AVAILABLE SAVE STATES ================");
            Console.WriteLine($"{"Idx",-5} | {"Date/Time",-19} | {"Type",-8} | {"Window Title / Detail"}");
            Console.WriteLine(new string('-', 75));

            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                var relativePath = file.FullName.Contains("NotepadBackups") ? "Notepad" : "Terminal";
                string displayName = ParseDisplayName(file.Name);
                string dateStr = file.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss");

                Console.WriteLine($"{i + 1,-5} | {dateStr,-19} | {relativePath,-8} | {displayName}");
            }
            Console.WriteLine("=======================================================\n");
        }

        private static void RestoreInteractive()
        {
            var files = GetSortedBackups();
            if (files.Count == 0)
            {
                Console.WriteLine("No save states found to restore.");
                return;
            }

            ListSaveStates();

            while (true)
            {
                Console.Write("Enter the index of the save state to restore (or 'q' to quit): ");
                string? input = Console.ReadLine()?.Trim();
                if (string.Equals(input, "q", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (int.TryParse(input, out int idx) && idx >= 1 && idx <= files.Count)
                {
                    var selectedFile = files[idx - 1];
                    Console.WriteLine($"\nRestoring: {selectedFile.Name}...");

                    if (selectedFile.FullName.Contains("NotepadBackups"))
                    {
                        // Open in notepad
                        try
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = "notepad.exe",
                                Arguments = $"\"{selectedFile.FullName}\"",
                                UseShellExecute = true
                            };
                            Process.Start(psi);
                            Console.WriteLine("Opened save state in Notepad.\n");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to open Notepad: {ex.Message}\n");
                        }
                    }
                    else
                    {
                        // Print terminal output to console
                        try
                        {
                            string text = File.ReadAllText(selectedFile.FullName);
                            Console.WriteLine("\n--- BEGIN TERMINAL SNAPSHOT ---");
                            Console.WriteLine(text);
                            Console.WriteLine("--- END TERMINAL SNAPSHOT ---\n");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to read backup: {ex.Message}\n");
                        }
                    }
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid selection. Please try again.");
                }
            }
        }

        private static List<FileInfo> GetSortedBackups()
        {
            var list = new List<FileInfo>();
            if (Directory.Exists(NotepadBackupDirectory))
            {
                list.AddRange(new DirectoryInfo(NotepadBackupDirectory).GetFiles("*.txt"));
            }
            if (Directory.Exists(TerminalBackupDirectory))
            {
                list.AddRange(new DirectoryInfo(TerminalBackupDirectory).GetFiles("*.txt"));
            }

            // Sort by LastWriteTime descending (newest first)
            list.Sort((a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
            return list;
        }

        private static string ParseDisplayName(string fileName)
        {
            // Format: yyyyMMdd_HHmmss_pid_title.txt
            try
            {
                int firstUnderscore = fileName.IndexOf('_');
                if (firstUnderscore < 0) return fileName;
                int secondUnderscore = fileName.IndexOf('_', firstUnderscore + 1);
                if (secondUnderscore < 0) return fileName;
                int thirdUnderscore = fileName.IndexOf('_', secondUnderscore + 1);
                if (thirdUnderscore < 0) return fileName;

                string titleWithExt = fileName.Substring(thirdUnderscore + 1);
                string title = Path.GetFileNameWithoutExtension(titleWithExt);
                return title;
            }
            catch
            {
                return fileName;
            }
        }

        private static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(@"=================================================================");
            Console.WriteLine(@"  ____                           _        _                      ");
            Console.WriteLine(@" / ___|  __ ___   _ ___ ___  ___| |_ __ _| |_ ___                ");
            Console.WriteLine(@" \___ \ / _` \ \ / / _ / __|/ _ | __/ _` | __/ _ \               ");
            Console.WriteLine(@"  ___) | (_| |\ V /  __\__ |  __| || (_| | ||  __/               ");
            Console.WriteLine(@" |____/ \__,_| \_/ \___|___/\___|\__\__,_|\__\___|  Logger       ");
            Console.WriteLine(@"=================================================================");
            Console.ResetColor();
        }

        private static void Log(string message, bool isError = false)
        {
            string formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{(isError ? "ERROR" : "INFO")}] {message}";

            // Console output
            if (isError)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(formattedMessage);
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine(formattedMessage);
            }

            // File output (COM/Multi-process safety)
            try
            {
                Directory.CreateDirectory(RootBackupDirectory);
                File.AppendAllText(LogFilePath, formattedMessage + Environment.NewLine);
            }
            catch
            {
                // Fallback: ignore log-write errors to prevent service failure
            }
        }
    }
}