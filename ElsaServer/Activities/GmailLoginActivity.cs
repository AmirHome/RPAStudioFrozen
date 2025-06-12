using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
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
        private IBrowserContext? _browserContext;        private ILogger<GmailLoginActivity>? _logger;        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            _logger = context.GetRequiredService<ILogger<GmailLoginActivity>>();
            var email = Email.Get(context);
            var password = Password.Get(context);
            var cookieFile = CookieStoragePath.Get(context);
            var maxRetries = MaxRetries.Get(context);
            var retryCount = 0;
            var success = false;

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
                            Headless = false // Set to true in production
                        });
                        _browserContext = await _browser.NewContextAsync();
                        _page = await _browserContext.NewPageAsync();

                        if (_page != null)
                        {
                            // Navigate to Gmail signin
                            await _page.GotoAsync("https://accounts.google.com/signin");
                            
                            // Enter email
                            await _page.FillAsync("[type='email']", email);
                            await _page.ClickAsync("#identifierNext");
                            
                            // Wait for password field and enter password
                            await _page.WaitForSelectorAsync("[type='password']", new() { Timeout = 100000 });
                            await _page.FillAsync("[type='password']", password);
                            await _page.ClickAsync("#passwordNext");

                            // Wait for successful login by checking for Gmail inbox
                            try 
                            {
                                await _page.WaitForURLAsync("https://mail.google.com/mail/*/inbox", new() { Timeout = 30000 });
                                
                                // Store cookies
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
                        await Task.Delay(5000); // Wait before retrying
                    }
                }
            }

            await CleanupAsync();
            Success.Set(context, success);
            await context.CompleteActivityAsync();
        }

        private async Task CleanupAsync()
        {
            if (_page != null) await _page.CloseAsync();
            if (_browserContext != null) await _browserContext.DisposeAsync();
            if (_browser != null) await _browser.DisposeAsync();
            if (_playwright != null) _playwright.Dispose();
        }
    }
}
