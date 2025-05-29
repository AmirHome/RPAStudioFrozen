using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;

namespace ElsaServer.Activities;

[Activity("MyEventTrigger", "AmirHoss", "Executes one of two branches based on a condition.")]

public class MyEventTrigger : Trigger
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        if (context.IsTriggerOfWorkflow())
        {
            await context.CompleteActivityAsync();
            return;
        }

        context.CreateBookmark("MyEvent");
    }

    protected override object GetTriggerPayload(TriggerIndexingContext context)
    {
        return "MyEvent";
    }
}
