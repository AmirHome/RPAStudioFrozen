using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace ElsaServer.Activities
{
    [Activity("ClickTestOnWeb", "AH-Playwright", "Performs a test click on Playwright website")]    public class ClickTestOnWeb : Activity
    {
        [Input(Description = "Run the browser in headless mode (no UI). Defaults to true.", Category = "Browser Settings", DefaultValue = true)]
        public Input<bool> Headless { get; set; } = new(true);

        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IPage? _page;

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            var headless = Headless.Get(context);
            var logger = context.GetRequiredService<ILogger<ClickTestOnWeb>>();
            try
            {
                // Initialize Playwright
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions {
                    Headless = headless
                });
                _page = await _browser.NewPageAsync();

                // Navigate to Playwright website
                await _page.GotoAsync("https://playwright.dev");

                // Verify title contains "Playwright"
                await Microsoft.Playwright.Assertions.Expect(_page).ToHaveTitleAsync(new Regex("Playwright"));

                // Click the get started link
                await _page.GetByRole(AriaRole.Link, new() { Name = "Get started" }).ClickAsync();

                // Verify Installation heading is visible by Console.WriteLine
                await Microsoft.Playwright.Assertions.Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Installation" })).ToBeVisibleAsync();
                logger.LogInformation("Successfully clicked the 'Get started' link and verified the 'Installation' heading is visible.");


            }
            catch (PlaywrightException ex)
            {
                logger.LogWarning("Failed to handle cookie consent: {Message}", ex.Message);
            }
            finally
            {
                // Cleanup
                if (!headless && _browser != null) // Keep browser open for a bit if not headless
                {
                    logger.LogInformation("Keeping browser open for 5 seconds in non-headless mode.");
                    await Task.Delay(5000);
                }
                if (_page != null) await _page.CloseAsync();
                if (_browser != null) await _browser.DisposeAsync();
                if (_playwright != null) _playwright.Dispose();

            }

            await context.CompleteActivityAsync();
        }
    }
}
