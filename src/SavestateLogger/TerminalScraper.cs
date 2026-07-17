using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Automation;

namespace SavestateLogger
{
    public class TerminalScraper
    {
        private static readonly string[] TerminalProcessNames = { "cmd", "powershell", "pwsh", "WindowsTerminal" };

        /// <summary>
        /// Retrieves all active command line or terminal processes with valid window handles.
        /// </summary>
        public static List<Process> GetActiveTerminalProcesses()
        {
            var result = new List<Process>();
            foreach (var name in TerminalProcessNames)
            {
                var processes = Process.GetProcessesByName(name);
                foreach (var process in processes)
                {
                    try
                    {
                        process.Refresh();
                        if (!process.HasExited && process.MainWindowHandle != IntPtr.Zero)
                        {
                            result.Add(process);
                        }
                    }
                    catch
                    {
                        // Handle access denied or exit race conditions
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Extracts the screen buffer text from a running terminal process using Windows UI Automation.
        /// </summary>
        public static string ExtractText(Process process)
        {
            if (process == null)
                throw new ArgumentNullException(nameof(process));

            process.Refresh();
            if (process.MainWindowHandle == IntPtr.Zero)
                throw new InvalidOperationException("Process does not have a main window handle.");

            AutomationElement windowElement = AutomationElement.FromHandle(process.MainWindowHandle);
            if (windowElement == null)
                throw new InvalidOperationException("Failed to find AutomationElement from window handle.");

            // Search for Document or Edit control types (covers classic conhost and Windows Terminal)
            Condition condition = new OrCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document),
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit)
            );

            AutomationElement? textElement = windowElement.FindFirst(TreeScope.Descendants, condition);
            
            // If not found, try to recursively search for any element supporting TextPattern or ValuePattern
            if (textElement == null)
            {
                textElement = FindElementSupportingPattern(windowElement);
            }

            // If still not found, fallback to the main window element itself
            if (textElement == null)
            {
                textElement = windowElement;
            }

            // Strategy 1: TextPattern (Standard for console buffers and rich text terminal windows)
            if (textElement.TryGetCurrentPattern(TextPattern.Pattern, out object patternObj))
            {
                TextPattern textPattern = (TextPattern)patternObj;
                return textPattern.DocumentRange.GetText(-1);
            }

            // Strategy 2: ValuePattern (Standard fallback for simpler edit boxes/classic consoles)
            if (textElement.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePatternObj))
            {
                ValuePattern valuePattern = (ValuePattern)valuePatternObj;
                return valuePattern.Current.Value;
            }

            throw new InvalidOperationException("Terminal window does not support TextPattern or ValuePattern.");
        }

        /// <summary>
        /// Helper to traverse the UI automation tree and locate any element that supports Text/Value patterns.
        /// </summary>
        private static AutomationElement? FindElementSupportingPattern(AutomationElement root)
        {
            var walker = TreeWalker.RawViewWalker;
            return SearchElement(root, walker);
        }

        private static AutomationElement? SearchElement(AutomationElement element, TreeWalker walker)
        {
            if (element == null) return null;

            if (element.TryGetCurrentPattern(TextPattern.Pattern, out _) || 
                element.TryGetCurrentPattern(ValuePattern.Pattern, out _))
            {
                return element;
            }

            try
            {
                for (AutomationElement? child = walker.GetFirstChild(element); child != null; child = walker.GetNextSibling(child))
                {
                    var result = SearchElement(child, walker);
                    if (result != null) return result;
                }
            }
            catch
            {
                // Handle occasional UI Automation COM errors gracefully
            }

            return null;
        }
    }
}