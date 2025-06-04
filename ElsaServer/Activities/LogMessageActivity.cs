using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Extensions;

namespace ElsaServer.Activities
{
    [Activity("Logging", "AmirHoss", "Logs a custom message to the console")]
    public class LogMessageActivity : CodeActivity
    {
        public Input<string> Message { get; set; } = default!;
        public Output<decimal> Result { get; set; } = default!;

        protected override void Execute(ActivityExecutionContext context)
        {
            var logger = context.GetRequiredService<ILogger<LogMessageActivity>>();
            string? messageString = null;
            if (Message != null)
                messageString = Message.Get(context);

            var randomNumber = Random.Shared.Next(10, 100);
            Result.Set(context, randomNumber);

            if (!string.IsNullOrWhiteSpace(messageString))
                logger.LogInformation($"Logger:\"{messageString}\" - Random Number: {randomNumber}");
            else
                logger.LogWarning("LogMessageActivity: No message provided.");
        }
    }
}
