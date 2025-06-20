using Elsa.Workflows;
using Elsa.Workflows.Activities;

namespace ElsaServer.Activities;

public class GenerateRandomNumberDirectAccessWorkflow : WorkflowBase
{
    protected override void Build(IWorkflowBuilder builder)
    {
        var randomNumber = builder.WithVariable("RandomNumber", 0m);

        builder.Name= "Workflow Log Random Number DA";
        builder.Description = "Log Random Number Direct Access";

        builder.Root = new Sequence
        {
            Activities =
            {
                new LogMessageActivity
                {
                    Name = "LogMessageActivity1",
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
