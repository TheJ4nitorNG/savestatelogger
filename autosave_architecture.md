# Architecture and Tech Stack for Application State Autosave

The core concept of a universal, system-level autosave to prevent data loss from crashes or power outages is highly practical. However, relying on a keylogger to build a "save state" is structurally flawed. This document outlines why a keylogger should be avoided, how to architect the tool correctly using state capture, and the recommended tech stacks for Windows.

## Why a Keylogger is the Wrong Tool for the Job

A keylogger only records keystrokes; it does not understand context or state. It fails for this use case due to the following reasons:

* **It misses mouse interactions:** If a user highlights a massive block of code and clicks "Delete," or uses the mouse to copy and paste text from a browser into Notepad, a keylogger captures none of that. The save state will be completely out of sync with reality.
* **It misses terminal output:** In PowerShell or CMD, a keylogger only saves the commands typed, not the output the system returned, nor the current working directory. It does not save the terminal's state, only a script of inputs.
* **It triggers security software:** Modern operating systems and endpoint protection (like Windows Defender) will instantly flag, quarantine, or kill a background process hooking into global keyboard inputs.

## The Better Architecture: State Capture

Instead of recording *how* the text got there (keystrokes), the tool needs to capture the *current state* of the application directly. 

### 1. For Notepad and Text Editors: UI Automation (UIA)
Microsoft provides an Accessibility framework called UI Automation (UIA). While designed for screen readers, developers use it to programmatically inspect and interact with UI elements. The tool can use UIA to:
1. Find the active Notepad (or other editor) window.
2. Target the main text editing block.
3. Dump the exact text currently in the editor to a local backup file every 10 minutes.

### 2. For Terminals (CMD / PowerShell): ConPTY or Shell Hooks
Terminals are harder to scrape because they are essentially infinite scrolling grids of text. 

* **The Shell Hook approach:** For PowerShell, a script can be written to modify the `PSReadLine` module to automatically append every executed command and its current directory to a persistent log file.
* **The Wrapper approach (Advanced):** The Windows Pseudo Console (ConPTY) API can be used. The tool acts as a middleman, launching CMD or PowerShell inside a hidden ConPTY instance. This allows the program to intercept and save both the user's input and the terminal's output stream as it happens.

---

## The Recommended Tech Stack

Since the targets (Notepad, PowerShell, CMD) indicate a Windows environment, tools designed to interface directly with the Windows API are required.

### Option 1: C# and .NET (Highly Recommended)
This is the native ecosystem for Windows desktop development and the best choice for a stable, performant background service.

* **Core Logic:** C# 
* **Framework:** .NET 8 or .NET Framework (depending on backwards compatibility needs).
* **Libraries:** * `UIAutomationClient` and `UIAutomationTypes` (Built-in to .NET) for scraping text from Notepad and GUI editors.
    * `System.Management.Automation` for interfacing with PowerShell instances.
    * P/Invoke to call raw Win32 APIs if building a ConPTY wrapper.

### Option 2: Python (Best for Prototyping)
Python has excellent wrapper libraries for the Windows APIs, making it ideal for a quick proof-of-concept.

* **Core Logic:** Python 3.x
* **Libraries:**
    * `uiautomation` or `pywinauto`: Third-party packages that simplify interacting with the Windows UIA framework compared to raw C#/C++. 
    * `pywin32`: For interacting with lower-level Windows APIs.
    * `pyinstaller`: To package the final script into a standalone executable that runs in the background.
