using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Automation;

namespace SavestateLogger
{
    public class NotepadScraper
    {
        /// <summary>
        /// Retrieves all currently running notepad.exe processes with active windows.
        /// </summary>
        public static List<Process> GetActiveNotepadProcesses()
        {
            var result = new List<Process>();
            var processes = Process.GetProcessesByName("Notepad");
            foreach (var process in processes)
            {
                try
                {
                    // Refresh the process state to get accurate handle info
                    process.Refresh();
                    if (!process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                    {
                        result.Add(process);
                    }
                }
                catch
                {
                    // Handle edge cases like process exited or access denied
                }
            }
            return result;
        }

        /// <summary>
        /// Extracts the current text contents of a Notepad process using Windows UI Automation.
        /// </summary>
        public static string ExtractText(Process process)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            process.Refresh();
            if (process.MainWindowHandle == IntPtr.Zero)
                throw new InvalidOperationException("Process does not have a main window handle.");

            // Bind to the Notepad main window
            AutomationElement windowElement = AutomationElement.FromHandle(process.MainWindowHandle);
            if (windowElement == null)
                throw new InvalidOperationException("Failed to find AutomationElement from window handle.");

            // Search for Document or Edit control types (universal across Windows versions)
            Condition condition = new OrCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit)
            );

            AutomationElement textElement = windowElement.FindFirst(TreeScope.Descendants, condition);
            if (textElement == null)
            {
                // Fallback: search for ClassNameProperty of "RichEditD2DPT" (Win11) or "Edit" (Win10)
                Condition fallbackCondition = new OrCondition(
                    new PropertyCondition(AutomationElement.ClassNameProperty, "RichEditD2DPT"),
                    new PropertyCondition(AutomationElement.ClassNameProperty, "Edit")
                );
                textElement = windowElement.FindFirst(TreeScope.Descendants, fallbackCondition);
            }

            if (textElement == null)
                throw new InvalidOperationException("Failed to locate text editing control in Notepad.");

            // Strategy 1: TextPattern (Standard for Windows 11 / RichEdit)
            if (textElement.TryGetCurrentPattern(TextPattern.Pattern, out object patternObj))
            {
                TextPattern textPattern = (TextPattern)patternObj;
                return textPattern.DocumentRange.GetText(-1);
            }

            // Strategy 2: ValuePattern (Standard for Windows 10 / Classic Edit)
            if (textElement.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj))
            {
                ValuePattern valuePattern = (ValuePattern)valuePatternObj;
                return valuePattern.Current.Value;
            }

            throw new InvalidOperationException("Text editing control does not support TextPattern or ValuePattern.");
        }
    }
}