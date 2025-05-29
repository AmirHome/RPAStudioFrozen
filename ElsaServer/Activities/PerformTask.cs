using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Activities.Flowchart.Attributes;

namespace ElsaServer.Activities;

[Activity("PassFail", "AmirHoss", "Logs a custom message to the consolex")]


[FlowNode("Pass", "Fail")]
public class PerformTask : Activity
{
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        await context.CompleteActivityWithOutcomesAsync("Pass");
    }
}
