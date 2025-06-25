using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace ElsaServer.Activities
{
    [Activity("YssLoginPlaywrightPlaywright", "AH-Playwright", "Performs a test click on Playwright website")]

    public class YssLoginPlaywrightPlaywright : Activity
    {
        [Input(Description = "YSS sisteminde Takip Bilgileri Giriş", Category = "Browser Settings", DefaultValue = true)]
        public Input<bool> Headless { get; set; } = new(true);

        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IPage? _page;

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            var headless = Headless.Get(context);
            var logger = context.GetRequiredService<ILogger<YssLoginPlaywrightPlaywright>>();
            string url = "https://alacaktakip.ic-a.com.tr/";
            try
            {
                // Initialize Playwright
                _playwright = await Playwright.CreateAsync();
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = headless
                });
                _page = await _browser.NewPageAsync();

                // Navigate to website
                await _page.GotoAsync(url);

                // Verify title contains website
                await Microsoft.Playwright.Assertions.Expect(_page).ToHaveTitleAsync(new Regex("Playwright"));

                // Click the get started link
                await _page.GetByRole(AriaRole.Link, new() { Name = "Get started" }).ClickAsync();

                // Verify Installation heading is visible by Console.WriteLine
                await Microsoft.Playwright.Assertions.Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Installation" })).ToBeVisibleAsync();
                logger.LogInformation("Successfully clicked the 'Get started' link and verified the 'Installation' heading is visible.");


            }
            catch (PlaywrightException ex)
            {
                logger.LogWarning("Failed to consent: {Message}", ex.Message);
                throw new Exception($"Failed to visit website {url}", ex);
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
