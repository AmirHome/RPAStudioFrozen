using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using System.Text.Json;

namespace ElsaServer.Activities
{
    [Activity("GmailLogin", "AH-Playwright", "Logs into Gmail and stores session cookies")]
    public class GmailLoginActivity : Activity
    {
        [Input(Description = "Gmail email address")]
        public Input<string> Email { get; set; } = default!;

        [Input(Description = "Gmail password")]
        public Input<string> Password { get; set; } = default!;

        [Input(Description = "Cookie storage file path", DefaultValue = "gmail_cookies.json")]
        public Input<string> CookieStoragePath { get; set; } = new("gmail_cookies.json");

        [Input(Description = "Maximum retry attempts", DefaultValue = 3)]
        public Input<int> MaxRetries { get; set; } = new(3);

        [Output(Description = "Login success status")]
        public Output<bool> Success { get; set; } = default!;

        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IPage? _page;
        private ILogger<GmailLoginActivity>? _logger;
        private IBrowserContext? _browserContext; // Moved browser context to class level

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            _logger = context.GetRequiredService<ILogger<GmailLoginActivity>>();
            var email = Email.Get(context);
            var password = Password.Get(context);
            var cookieFile = CookieStoragePath.Get(context);
            var maxRetries = MaxRetries.Get(context);
            var retryCount = 0;
            var success = false;

            // Check if cookie file exists
            if (File.Exists(cookieFile))
            {
                _logger.LogInformation($"Cookie file '{cookieFile}' exists. Skipping login.");
                _logger.LogDebug("Loading debug.");
                _logger.LogTrace("Loading trace.");
                Success.Set(context, true);
                await context.CompleteActivityAsync();
                return;
            }

            while (retryCount < maxRetries && !success)
            {
                try
                {
                    retryCount++;
                    _logger.LogInformation("Gmail login attempt {Count} of {Max}", retryCount, maxRetries);

                    // Initialize Playwright
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
                            }
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

                        if (_page != null)
                        {
                            await _page.GotoAsync("https://mail.google.com/mail");
                            await _page.FillAsync("[type='email']", email, new() { Timeout = 10000 });
                            await Task.Delay(1000);
                            await _page.ClickAsync("#identifierNext");
                            await _page.WaitForSelectorAsync("[type='password']", new() { Timeout = 100000 });
                            await _page.FillAsync("[type='password']", password);
                            await _page.ClickAsync("#passwordNext");
                            try
                            {
                                await _page.WaitForURLAsync("https://mail.google.com/mail/u/0/#inbox", new() { Timeout = 30000 });
                                if (_browserContext != null)
                                {
                                    var cookies = await _browserContext.CookiesAsync();
                                    await File.WriteAllTextAsync(cookieFile, JsonSerializer.Serialize(cookies));
                                    success = true;
                                    _logger.LogInformation("Successfully logged into Gmail and stored cookies");
                                }
                            }
                            catch (TimeoutException)
                            {
                                _logger.LogWarning("Login verification timed out");
                                throw;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed login attempt {Count}", retryCount);
                    await CleanupAsync();
                    if (retryCount < maxRetries)
                    {
                        await Task.Delay(5000);
                    }
                }
            }

            await CleanupAsync();
            Success.Set(context, success);
            await context.CompleteActivityAsync();
        }

        private async Task CleanupAsync()
        {
            try
            {
                if (_page != null) await _page.CloseAsync();
                if (_browser != null) await _browser.DisposeAsync();
                if (_playwright != null) _playwright.Dispose();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error during cleanup");
            }
        }
    }
}
