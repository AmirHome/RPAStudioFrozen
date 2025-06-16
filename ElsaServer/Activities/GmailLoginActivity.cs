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

        [Input(Description = "User data directory for persistent browser profile")]
        public Input<string?> UserDataDir { get; set; } = default!;

        [Output(Description = "Login success status")]
        public Output<bool> Success { get; set; } = default!;

        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IPage? _page;
        private ILogger<GmailLoginActivity>? _logger;

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            _logger = context.GetRequiredService<ILogger<GmailLoginActivity>>();

            var email = context.Get(Email);
            var password = context.Get(Password);
            var cookiePath = context.Get(CookieStoragePath);
            var userDataDir = context.Get(UserDataDir) ?? "playwright-user";
            var maxRetries = context.Get(MaxRetries);

            int retryCount = 0;
            bool success = false;

            while (retryCount < maxRetries && !success)
            {
                retryCount++;
                _logger.LogInformation("Gmail login attempt {Attempt} of {Max}", retryCount, maxRetries);

                try
                {
                    using var playwright = await Playwright.CreateAsync();
                    var browser = await playwright.Chromium.LaunchPersistentContextAsync(userDataDir, new BrowserTypeLaunchPersistentContextOptions
                    {
                        Headless = false,
                        Args = new[] {
                            "--disable-blink-features=AutomationControlled",
                            "--start-maximized"
                        },
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36"
                    });

                    var _page = browser.Pages.FirstOrDefault() ?? await browser.NewPageAsync();
                    await _page.GotoAsync("https://mail.google.com/");

                    await _page.FillAsync("input[type='email']", email);
                    await _page.ClickAsync("#identifierNext");
                    await _page.WaitForTimeoutAsync(2000);

                    await _page.FillAsync("input[type='password']", password);
                    await _page.ClickAsync("#passwordNext");

                    await _page.WaitForNavigationAsync(new PageWaitForNavigationOptions
                    {
                        UrlRegex = new System.Text.RegularExpressions.Regex("myaccount.google.com|mail.google.com")
                    });

                    if (_page.Url.Contains("myaccount.google.com") || _page.Url.Contains("mail.google.com"))
                    {
                        var cookies = await _page.Context.CookiesAsync();
                        var json = JsonSerializer.Serialize(cookies, new JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(cookiePath, json);
                        success = true;
                    }

                    await browser.CloseAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Login attempt failed: {Message}", ex.Message);
                    await Task.Delay(3000);
                }
            }

            context.Set(Success, success);
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
