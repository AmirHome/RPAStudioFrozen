using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace ElsaServer.Activities
{
    [Activity("ClickTestOnWeb", "AH-Playwright", "Performs a test click on Playwright website")]    public class ClickTestOnWeb : Activity
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IPage? _page;

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                // Initialize Playwright
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync();
                _page = await _browser.NewPageAsync();

                // Navigate to Playwright website
                await _page.GotoAsync("https://playwright.dev");

                // Verify title contains "Playwright"
                await Microsoft.Playwright.Assertions.Expect(_page).ToHaveTitleAsync(new Regex("Playwright"));

                // Click the get started link
                await _page.GetByRole(AriaRole.Link, new() { Name = "Get started" }).ClickAsync();

                // Verify Installation heading is visible by Console.WriteLine
                await Microsoft.Playwright.Assertions.Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Installation" })).ToBeVisibleAsync();
                Console.WriteLine("Yes, Installation heading is visible.");


            }
            finally
            {
                // Cleanup
                if (_page != null) await _page.CloseAsync();
                if (_browser != null) await _browser.DisposeAsync();
                if (_playwright != null) _playwright.Dispose();
                
            }

            await context.CompleteActivityAsync();
        }
    }
}
