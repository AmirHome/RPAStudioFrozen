using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using FlaUI.Core.Tools; // Added for Retry
using ElsaActivity = Elsa.Workflows.Activity;

namespace ElsaServer.Activities
{
    [Activity("NotepadAutomation", "AH-FlaUI", "Automates Notepad: opens, writes text, and saves a file on Desktop")]
    public class NotepadAutomationActivity : ElsaActivity
    {
        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                using (var automation = new UIA3Automation())
                {
                    var psi = new ProcessStartInfo("notepad.exe")
                    {
                        UseShellExecute = false
                    };
                    // Launch Notepad and allow FlaUI to manage the application lifecycle
                    var app = Application.Launch(psi); // Changed from AttachOrLaunch for more direct control if needed for retries

                    // Retry logic for getting the main window
                    var windowResult = Retry.WhileNull(() => app.GetMainWindow(automation), TimeSpan.FromSeconds(15), TimeSpan.FromMilliseconds(500));
                    var window = windowResult.Result ?? throw new Exception("Unable to locate Notepad main window after multiple attempts.");

                    window.Focus();
                    // Removed Task.Delay(1000)

                    // Retry logic for finding the edit element
                    var editElementResult = Retry.WhileNull(() =>
                    {
                        window.Focus(); // Ensure window is focused before searching
                        return window.FindFirstDescendant(cf => cf.ByClassName("Edit"))
                               ?? window.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit))
                               ?? window.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Document));
                    }, TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(500));
                    var editElement = editElementResult.Result ?? throw new Exception("Could not find text edit area after multiple attempts.");

                    var edit = editElement.AsTextBox();
                    edit.Focus();
                    await Task.Delay(100); // Keeping this short delay for focus stability before typing

                    string line1 = "Hello from FlaUI!";
                    string line2 = "This is an automated test.";
                    string line3 = $"Current time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    string textToEnter = $"{line1}\n{line2}\n{line3}";
                    edit.Text = textToEnter;

                    using (Keyboard.Pressing(VirtualKeyShort.CONTROL))
                    {
                        Keyboard.Type(VirtualKeyShort.KEY_S);
                    }

                    // Retry logic for the save dialog to appear
                    var saveDialogResult = Retry.WhileNull(() => window.ModalWindows.FirstOrDefault(), TimeSpan.FromSeconds(5));
                    var saveDialog = saveDialogResult.Result;

                    if (saveDialog != null)
                    {
                        saveDialog.Focus(); // Focus the dialog before interacting

                        // Retry logic for finding the file name text box
                        var fileNameElementResult = Retry.WhileNull(() =>
                            saveDialog.FindFirstDescendant(cf => cf.ByClassName("Edit"))
                            ?? saveDialog.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit)),
                            TimeSpan.FromSeconds(5));
                        var fileNameElement = fileNameElementResult.Result ?? throw new Exception("Could not find file name text box in save dialog.");
                        var fileNameBox = fileNameElement.AsTextBox();

                        string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "FlaUITest.txt");
                        fileNameBox.Enter(fileName); // Enter text

                        // Retry logic for finding the save button
                        var saveButtonElementResult = Retry.WhileNull(() => saveDialog.FindFirstDescendant(cf => cf.ByAutomationId("1")), TimeSpan.FromSeconds(5));
                        var saveButtonElement = saveButtonElementResult.Result ?? throw new Exception("Could not find save button in save dialog.");
                        var saveButton = saveButtonElement.AsButton();

                        saveButton.Invoke();
                    }
                    else
                    {
                        // If save dialog doesn't appear, perhaps the file was already saved or another issue occurred.
                        // For this example, we'll just log or note this. If it's an error, an exception could be thrown.
                        Console.WriteLine("Save dialog did not appear as expected.");
                        // Depending on desired behavior, may need to throw an exception here if dialog is critical
                    }

                    // Ensure app is closed, consider if it needs to be conditional on successful save or handled differently
                    if (!app.HasExited)
                    {
                        app.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception details here through ILogger if available, or rethrow as appropriate
                Console.WriteLine($"Error in Notepad automation: {ex.Message}");
                // Consider how to handle this in Elsa context, e.g., setting fault or specific output
                throw; // Rethrowing to allow Elsa to handle it as a fault
            }
            
            await context.CompleteActivityAsync(); // Ensure this is called appropriately based on success/failure
        }
    }
}
