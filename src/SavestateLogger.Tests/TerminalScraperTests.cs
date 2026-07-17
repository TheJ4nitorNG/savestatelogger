using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Xunit;
using Xunit.Abstractions;
using System.Windows.Forms;

namespace SavestateLogger.Tests
{
    public class TerminalScraperTests : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private Process? _terminalProcess;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        public TerminalScraperTests(ITestOutputHelper output)
        {
            _output = output;
            CleanUpTerminalProcesses();
        }

        private void CleanUpTerminalProcesses()
        {
            foreach (var process in Process.GetProcessesByName("cmd"))
            {
                try
                {
                    if (process.MainWindowTitle.Contains("TerminalScraperTest"))
                    {
                        process.Kill();
                        process.WaitForExit(2000);
                    }
                }
                catch
                {
                    // Ignore
                }
            }
        }

        [Fact]
        public void TestTerminalDiscoveryAndScraping()
        {
            // Arrange
            string testToken = "TDD_" + new Random().Next(100000, 999999).ToString();
            string testCommand = $"echo {testToken}";

            _terminalProcess = new Process();
            _terminalProcess.StartInfo.FileName = "cmd.exe";
            _terminalProcess.StartInfo.UseShellExecute = true;
            _terminalProcess.Start();

            // Wait safely for the console process to initialize and show its window
            int retries = 20;
            while (_terminalProcess.MainWindowHandle == IntPtr.Zero && retries > 0)
            {
                Thread.Sleep(200);
                _terminalProcess.Refresh();
                retries--;
            }

            IntPtr windowHandle = _terminalProcess.MainWindowHandle;
            Assert.NotEqual(IntPtr.Zero, windowHandle);

            // Set the window title so we can identify and clean it up if needed
            ShowWindow(windowHandle, SW_RESTORE);
            SetForegroundWindow(windowHandle);
            Thread.Sleep(500);

            SendKeys.SendWait("title TerminalScraperTest{ENTER}");
            Thread.Sleep(1000); // Wait for title to set

            // Run the echo command
            SetForegroundWindow(windowHandle);
            Thread.Sleep(500);
            SendKeys.SendWait(testCommand + "{ENTER}");
            Thread.Sleep(1500); // Wait for terminal output to settle

            // Act & Assert Part 1: Terminal Process Discovery
            var activeTerminals = TerminalScraper.GetActiveTerminalProcesses();
            Assert.Contains(activeTerminals, p => p.Id == _terminalProcess.Id);

            // Act & Assert Part 2: Extract Terminal Text
            string scrapedText = TerminalScraper.ExtractText(_terminalProcess);
            _output.WriteLine("================ SCRAPED TEXT ================");
            _output.WriteLine(scrapedText);
            _output.WriteLine("==============================================");

            Assert.Contains(testToken, scrapedText);
        }

        public void Dispose()
        {
            if (_terminalProcess != null)
            {
                try
                {
                    if (!_terminalProcess.HasExited)
                    {
                        _terminalProcess.Kill();
                        _terminalProcess.WaitForExit(1000);
                    }
                    _terminalProcess.Dispose();
                }
                catch
                {
                    // Ignore
                }
            }
            CleanUpTerminalProcesses();
        }
    }
}