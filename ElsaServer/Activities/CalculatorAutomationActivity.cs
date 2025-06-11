using System;
using System.Diagnostics;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Tools; // Added for Retry
using FlaUI.UIA3;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using ElsaActivity = Elsa.Workflows.Activity;

namespace ElsaServer.Activities
{
    [Activity("CalculatorAutomation", "AH-FlaUI", "محاسبه با ماشین حساب ویندوز با FlaUI")]
    public class CalculatorAutomationActivity : ElsaActivity
    {
        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            var process = Process.Start("calc.exe");

            using (var app = Application.Attach(process))
            using (var automation = new UIA3Automation())
            {
                var windowResult = Retry.WhileNull(() => app.GetMainWindow(automation), TimeSpan.FromSeconds(5));
                var window = windowResult.Result;

                if (window != null)
                {
                    window.Focus();
                    FlaUI.Core.Input.Keyboard.Type("100000");
                    FlaUI.Core.Input.Keyboard.Type("*");
                    FlaUI.Core.Input.Keyboard.Type("0.02");
                    FlaUI.Core.Input.Keyboard.Type("=");

                    var resultLabel = Retry.WhileNull(() => window.FindFirstDescendant(cf => cf.ByAutomationId("CalculatorResults"))?.AsLabel(), TimeSpan.FromSeconds(5)).Result;
                    var resultText = Retry.WhileStringEmpty(() => resultLabel?.Text, TimeSpan.FromSeconds(5)).Result;

                    Console.WriteLine("Result: " + resultText);
                }
                else
                {
                    Console.WriteLine("Calculator window not found within the timeout period.");
                }
            }
            await context.CompleteActivityAsync();
        }
    }
}
