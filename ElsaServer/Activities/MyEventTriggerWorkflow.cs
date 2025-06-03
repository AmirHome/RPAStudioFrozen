using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
namespace ElsaServer.Activities;

public class MyEventTriggerWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {

        builder.Name = "Workflow MyEvent Trigger";

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
