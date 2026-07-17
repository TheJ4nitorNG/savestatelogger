# Project Overview: Savestate Logger

## Goal
Create a savestate for Notepad and PowerShell/CMD that creates a savestate for everything you've done every 10 minutes. If your PC loses power you don't lose progress, if you forgot to save, it doesn't matter, and if you screw up your code you can just load the previous save state.

## Tech Stack
*   **Language/Framework Options:** C# (.NET 8 or .NET Framework) OR Python 3.x
*   **C# Libraries:** `UIAutomationClient`, `UIAutomationTypes`, `System.Management.Automation`, P/Invoke
*   **Python Libraries:** `uiautomation` or `pywinauto`, `pywin32`, `pyinstaller`
*   **Note:** Chosen stack will prioritize production-ready capabilities for Windows API manipulation.