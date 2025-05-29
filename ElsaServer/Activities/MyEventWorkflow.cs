using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace ElsaServer.Activities;

public class MyEventWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new WriteLine("Starting workflow..."),
                new MyEvent(),
                new WriteLine("Event occurred!")
            }
        };
    }
}
