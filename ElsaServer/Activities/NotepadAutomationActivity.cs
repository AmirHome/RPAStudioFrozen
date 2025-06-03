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
using ElsaActivity = Elsa.Workflows.Activity;

namespace ElsaServer.Activities
{
    [Activity("NotepadAutomation", "AmirHoss", "Automates Notepad: opens, writes text, and saves a file on Desktop")]
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
                    var initialApp = Application.AttachOrLaunch(psi);
                    await Task.Delay(2500);

                    Application app;
                    try
                    {
                        app = Application.Attach("notepad");
                    }
                    catch
                    {
                        app = initialApp;
                    }

                    Window? window = null;
                    int attempts = 0;
                    const int maxAttempts = 10;
                    while (window == null && attempts < maxAttempts)
                    {
                        try { window = app.GetMainWindow(automation); } catch { }
                        if (window == null) { attempts++; await Task.Delay(1000); }
                    }
                    if (window == null)
                        throw new Exception("Unable to locate Notepad main window.");

                    window.Focus();
                    await Task.Delay(1000);

                    AutomationElement? editElement = null;
                    attempts = 0;
                    while (editElement == null && attempts < maxAttempts)
                    {
                        window.Focus();
                        editElement = window.FindFirstDescendant(cf => cf.ByClassName("Edit"))
                                     ?? window.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit))
                                     ?? window.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Document));
                        if (editElement == null) { attempts++; await Task.Delay(1500); }
                    }
                    if (editElement == null)
                        throw new Exception("Could not find text edit area.");

                    var edit = editElement.AsTextBox();
                    edit.Focus();
                    await Task.Delay(100);

                    string line1 = "Hello from FlaUI!";
                    string line2 = "This is an automated test.";
                    string line3 = $"Current time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                    string textToEnter = $"{line1}\n{line2}\n{line3}";
                    edit.Text = textToEnter;

                    using (Keyboard.Pressing(VirtualKeyShort.CONTROL))
                    {
                        Keyboard.Type(VirtualKeyShort.KEY_S);
                    }
                    await Task.Delay(1500);

                    Window? saveDialog = window.ModalWindows.FirstOrDefault();
                    if (saveDialog != null)
                    {
                        AutomationElement? fileNameElement = null;
                        attempts = 0;
                        while (fileNameElement == null && attempts < maxAttempts)
                        {
                            fileNameElement = saveDialog.FindFirstDescendant(cf => cf.ByClassName("Edit"))
                                            ?? saveDialog.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Edit));
                            if (fileNameElement == null) { attempts++; await Task.Delay(1000); }
                        }
                        if (fileNameElement == null)
                            throw new Exception("Could not find file name text box.");
                        var fileNameBox = fileNameElement.AsTextBox();
                        string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "FlaUITest.txt");
                        fileNameBox.Enter(fileName);

                        AutomationElement? saveButtonElement = null;
                        attempts = 0;
                        while (saveButtonElement == null && attempts < maxAttempts)
                        {
                            saveButtonElement = saveDialog.FindFirstDescendant(cf => cf.ByAutomationId("1"));
                            if (saveButtonElement == null) { attempts++; await Task.Delay(1000); }
                        }
                        if (saveButtonElement == null)
                            throw new Exception("Could not find save button.");
                        var saveButton = saveButtonElement.AsButton();
                        saveButton.Invoke();
                    }
                    app.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Notepad automation: {ex.Message}");
                throw;
            }
            
            await context.CompleteActivityAsync();
        }
    }
}
