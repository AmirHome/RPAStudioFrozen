using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.Activities;
using Elsa.Expressions;
using Microsoft.Extensions.Logging;
using Elsa.Extensions;

namespace ElsaServer.Activities
{
    [Activity("Logging", "AmirHoss", "Logs a custom message to the console")]
    public class LogMessageActivity : CodeActivity
    {
        public Input<string> Message { get; set; } = default!;

        protected override void Execute(ActivityExecutionContext context)
        {
            var logger = context.GetRequiredService<ILogger<LogMessageActivity>>();
            var messageString = Message.Get(context);
            if (!string.IsNullOrWhiteSpace(messageString))
                logger.LogInformation($"Logger:\"{messageString}\"");
            else
                logger.LogWarning("LogMessageActivity: No message provided.");
        }
    }
}
