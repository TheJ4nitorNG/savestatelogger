# Track 001 Plan: Core Service & Notepad State Capture

## Objective
Set up the C# background loop and implement UI Automation to extract text from running Notepad processes.

## Tasks
- [x] Initialize a .NET 8 Worker Service or Console App with a robust 10-minute timer loop.
- [x] Implement process discovery to find all active `notepad.exe` instances.
- [x] Use `UIAutomationClient` to target the main text `Document` or `Edit` control within each Notepad instance.
- [x] Extract the full text string and write it to a timestamped backup file in a local app data folder.