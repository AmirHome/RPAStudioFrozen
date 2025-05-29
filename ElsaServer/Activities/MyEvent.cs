using Elsa.Workflows;

using Elsa.Workflows.Attributes;

namespace ElsaServer.Activities;

[Activity("MyEvent", "AmirHoss", "Executes one of two branches based on a condition.")]
public class MyEvent : Activity
{
    protected override void Execute(ActivityExecutionContext context)
    {
        context.CreateBookmark("MyEvent");
    }
}
