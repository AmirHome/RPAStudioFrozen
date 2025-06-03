using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace ElsaServer.Activities;

public class IfWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        builder.Name= "Workflow IF";

        builder.Root = new If
        {
            Condition = new(context => DateTime.Now.IsDaylightSavingTime()),
            Then = new WriteLine("Hello to the light side!"),
            Else = new WriteLine("Hello to the dark side!")
        };
    }
}
