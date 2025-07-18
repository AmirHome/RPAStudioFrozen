using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
namespace ElsaServer.Activities;

public class GenerateRandomNumberWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var randomNumber = builder.WithVariable("RandomNumber", 0m);
        builder.Name= "Workflow Log Random Number";
        builder.Description = "Log Random Number";

        builder.Root = new Sequence
        {
            Activities =
            {
                new LogMessageActivity
                {
                    Message = new("Generating a random number..."),
                    Result = new(randomNumber)
                },
                new LogMessageActivity
                {
                    Message = new(context => $"The random number is: {randomNumber.Get(context)}")
                }
            }
        };
    }
}
