# Scratchpad

Progressively logging every action, hypothesis, and result for the Savestate Logger project.

## Track 001: Core Service & Notepad State Capture
*   **Hypothesis:** A background worker service in .NET 8 can periodically query UI Automation properties of open `notepad.exe` processes to capture current text states safely and efficiently.
*   **Result:** [SUCCESS] Implemented `NotepadScraper` using UI Automation supporting both classic and Windows 11 tabbed Notepad, validated with robust integration tests.

## Track 002: Terminal State Capture
*   **Hypothesis:** Like Notepad, active terminals (CMD and PowerShell running in `conhost.exe` or `WindowsTerminal.exe`) expose their active screen buffer to Windows UI Automation. We can target these elements and extract the current terminal output grid programmatically.
*   **Result:** [SUCCESS] Implemented `TerminalScraper` using UI Automation, supporting classic Conhost and Windows Terminal, integrated with the main sweep loop.

## Track 003: Restoration & Polish
*   **Hypothesis:** Adding file-based log streams, automated retention cleanup (e.g. deleting files > 7 days), and an interactive CLI restorer (`--list` / `--restore`) inside `Program.cs` completes a production-ready system.
*   **Plan:**
    1. Implement failing unit test in `BackupManagerTests.cs` for a `CleanupOldBackups` method that deletes mock files older than 7 days but preserves newer ones.
    2. Write production logic in `BackupManager.cs` to pass the cleanup test.
    3. Implement structured file logging to `%APPDATA%\SavestateLogger\service.log` to track operational health and exceptions.
    4. Implement CLI arguments `--list` (to display all states) and `--restore` (an interactive CLI menu to view/restore selected states) in `Program.cs`.
    5. Wire up the cleanup routine to run at the start of each sweep cycle.
    6. Validate the entire system and build.