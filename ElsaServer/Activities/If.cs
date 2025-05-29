using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.Attributes;

namespace ElsaServer.Activities;

[Activity("If", "AmirHoss", "Executes one of two branches based on a condition.")]

public class If : Activity
{

    public Input<bool> Condition { get; set; } = default!;
    public IActivity? Then { get; set; }
    public IActivity? Else { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var result = context.Get(Condition);
        var nextActivity = result ? Then : Else;
        await context.ScheduleActivityAsync(nextActivity, OnChildCompleted);
    }

    private async ValueTask OnChildCompleted(ActivityCompletedContext context)
    {
        await context.CompleteActivityAsync();
    }
}
