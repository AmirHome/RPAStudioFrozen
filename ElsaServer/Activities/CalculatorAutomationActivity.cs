using System;
using System.Diagnostics;
using System.Threading;
using FlaUI.Core;
using FlaUI.UIA3;
using FlaUI.Core.AutomationElements;
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
            Thread.Sleep(1000);

            using (var app = Application.Attach(process))
            using (var automation = new UIA3Automation())
            {
                var window = app.GetMainWindow(automation);
                if (window != null)
                {
                    window.Focus();
                    FlaUI.Core.Input.Keyboard.Type("100000");
                    FlaUI.Core.Input.Keyboard.Type("*");
                    FlaUI.Core.Input.Keyboard.Type("0.02");
                    FlaUI.Core.Input.Keyboard.Type("=");
                    Thread.Sleep(500);
                    var result = window.FindFirstDescendant(cf => cf.ByAutomationId("CalculatorResults"))?.AsLabel()?.Text;
                    Console.WriteLine("Result: " + result);
                }
                else
                {
                    Console.WriteLine("Calculator window not found.");
                }
            }
            await context.CompleteActivityAsync();
        }
    }
}
