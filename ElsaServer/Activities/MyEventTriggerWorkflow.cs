using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace ElsaServer.Activities;

public class MyEventTriggerWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Root = new Sequence
        {
            Activities =
            {
                new MyEventTrigger
                {
                    CanStartWorkflow = true
                },
                new WriteLine("Event occurred!")
            }
        };
    }
}
