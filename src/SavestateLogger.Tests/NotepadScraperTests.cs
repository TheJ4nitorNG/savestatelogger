using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;
using Xunit;
using System.Windows.Forms;

namespace SavestateLogger.Tests
{
    public class NotepadScraperTests : IDisposable
    {
        private Process? _notepadProcess;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9;

        public NotepadScraperTests()
        {
            CleanUpNotepadProcesses();
        }

        private void CleanUpNotepadProcesses()
        {
            foreach (var process in Process.GetProcessesByName("Notepad"))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(2000);
                }
                catch
                {
                    // Ignore failures on system/protected processes
                }
            }
        }

        [Fact]
        public void TestNotepadDiscoveryAndScraping()
        {
            // Arrange
            string testText = "Hello from Hyperagent TDD! " + Guid.NewGuid().ToString();
            
            // 1. Spawn a new Notepad instance
            _notepadProcess = new Process();
            _notepadProcess.StartInfo.FileName = "notepad.exe";
            _notepadProcess.Start();
            
            // Wait for Notepad to load and have an active window handle
            _notepadProcess.WaitForInputIdle(5000);
            int retries = 20;
            while (_notepadProcess.MainWindowHandle == IntPtr.Zero && retries > 0)
            {
                Thread.Sleep(200);
                _notepadProcess.Refresh();
                retries--;
            }

            IntPtr windowHandle = _notepadProcess.MainWindowHandle;
            if (windowHandle == IntPtr.Zero)
            {
                var candidates = Process.GetProcessesByName("Notepad");
                foreach (var c in candidates)
                {
                    c.Refresh();
                    if (c.MainWindowHandle != IntPtr.Zero)
                    {
                        _notepadProcess = c;
                        windowHandle = c.MainWindowHandle;
                        break;
                    }
                }
            }

            Assert.NotEqual(IntPtr.Zero, windowHandle);

            // Bind to the Notepad main window via UI Automation to input test text
            AutomationElement windowElement = AutomationElement.FromHandle(windowHandle);
            Assert.NotNull(windowElement);

            Condition condition = new OrCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit)
            );

            AutomationElement textElement = windowElement.FindFirst(TreeScope.Descendants, condition);
            if (textElement == null)
            {
                Condition fallbackCondition = new OrCondition(
                    new PropertyCondition(AutomationElement.ClassNameProperty, "RichEditD2DPT"),
                    new PropertyCondition(AutomationElement.ClassNameProperty, "Edit")
                );
                textElement = windowElement.FindFirst(TreeScope.Descendants, fallbackCondition);
            }

            Assert.NotNull(textElement);

            // Try to set text via ValuePattern (instant and robust)
            bool textSet = false;
            if (textElement.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj))
            {
                try
                {
                    ValuePattern valuePattern = (ValuePattern)valuePatternObj;
                    valuePattern.SetValue(testText);
                    textSet = true;
                }
                catch
                {
                    // Control might be read-only under ValuePattern (e.g. Win11 document)
                }
            }

            if (!textSet)
            {
                // Fallback: Bring to foreground, set focus, and use SendKeys
                ShowWindow(windowHandle, SW_RESTORE);
                SetForegroundWindow(windowHandle);
                Thread.Sleep(500);
                
                try
                {
                    textElement.SetFocus();
                    Thread.Sleep(200);
                }
                catch
                {
                    // Ignore focus errors
                }

                // Select all and replace if there's pre-existing text
                SendKeys.SendWait("^a");
                Thread.Sleep(200);
                SendKeys.SendWait("{BACKSPACE}");
                Thread.Sleep(200);

                SendKeys.SendWait(testText);
                Thread.Sleep(1000); // Wait for typing to register in UI
            }

            // Act & Assert Part 1: Process Discovery
            var processes = NotepadScraper.GetActiveNotepadProcesses();
            Assert.Contains(processes, p => p.Id == _notepadProcess.Id);

            // Act & Assert Part 2: Extracting Text
            string extractedText = NotepadScraper.ExtractText(_notepadProcess);
            Assert.Contains(testText, extractedText);
        }

        public void Dispose()
        {
            if (_notepadProcess != null)
            {
                try
                {
                    if (!_notepadProcess.HasExited)
                    {
                        _notepadProcess.Kill();
                        _notepadProcess.WaitForExit(1000);
                    }
                    _notepadProcess.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
            }
            CleanUpNotepadProcesses();
        }
    }
}