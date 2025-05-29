using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;

namespace ElsaServer.Activities
{
    [Activity("WriteLine", "AmirHoss", "Logs a custom message to the consolex")]
    public class PrintMessage : Activity
    {
        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            Console.WriteLine("Hello world!");
            await context.CompleteActivityAsync();
        }
    }
}