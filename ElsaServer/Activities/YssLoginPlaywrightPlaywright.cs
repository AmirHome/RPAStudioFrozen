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

        private IBrowserContext? _browserContext; // Moved browser context to class level
        
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
                // Find the path to Edge. Update this path if Edge is installed elsewhere.
                var edgePath = @"C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
                if (!System.IO.File.Exists(edgePath))
                    edgePath = @"C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe";

                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = headless,
                    ExecutablePath = edgePath
                });
                _page = await _browser.NewPageAsync();

                    _playwright = await Playwright.CreateAsync();
                if (_playwright != null)
                {
                    _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                    {
                        Headless = false, // Set to true in production
                        Args = new[]
                        {
                            "--disable-blink-features=AutomationControlled",
                            "--start-maximized"
                        },
                        ExecutablePath = edgePath
                    });
                    // Set browser context at the class level
                    this._browserContext = await _browser.NewContextAsync(new BrowserNewContextOptions
                    {
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36",
                        ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                        ScreenSize = new ScreenSize { Width = 1920, Height = 1080 },
                        Locale = "en-US",
                        TimezoneId = "America/New_York"
                    });
                    _page = await _browserContext.NewPageAsync();

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
