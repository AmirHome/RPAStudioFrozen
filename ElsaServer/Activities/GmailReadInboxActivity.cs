using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Playwright;
using System.Text.Json;

namespace ElsaServer.Activities
{
    [Activity("GmailReadInbox", "AH-Playwright", "Reads emails from Gmail inbox using stored session")]
    public class GmailReadInboxActivity : Activity
    {
        [Input(Description = "Cookie storage file path", DefaultValue = "gmail_cookies.json")]
        public Input<string> CookieStoragePath { get; set; } = new("gmail_cookies.json");

        [Input(Description = "Gmail email address for login fallback")]
        public Input<string> Email { get; set; } = default!;

        [Input(Description = "Gmail password for login fallback")]
        public Input<string> Password { get; set; } = default!;

        [Output(Description = "First email subject")]
        public Output<string> FirstEmailSubject { get; set; } = default!;

        [Output(Description = "First email sender")]
        public Output<string> FirstEmailSender { get; set; } = default!;

        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IPage? _page;
        private IBrowserContext? _browserContext;        private ILogger<GmailReadInboxActivity>? _logger;        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            try
            {
                _logger = context.GetRequiredService<ILogger<GmailReadInboxActivity>>();
                var cookieFile = CookieStoragePath.Get(context);
                
                // Initialize Playwright
                _playwright = await Playwright.CreateAsync();
                if (_playwright != null)
                {
                    _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                    {
                        Headless = false // Set to true in production
                    });
                    _browserContext = await _browser.NewContextAsync();

                    // Try to restore cookies
                    if (File.Exists(cookieFile))
                    {
                        try
                        {
                            var cookiesJson = await File.ReadAllTextAsync(cookieFile);
                            var cookies = JsonSerializer.Deserialize<List<Cookie>>(cookiesJson);
                            if (cookies != null && _browserContext != null)
                            {
                                await _browserContext.AddCookiesAsync(cookies);
                                _logger.LogInformation("Restored cookies from storage");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to restore cookies");
                        }
                    }

                    if (_browserContext != null)
                    {
                        _page = await _browserContext.NewPageAsync();
                        if (_page != null)
                        {
                            await _page.GotoAsync("https://mail.google.com");

                            // Check if we need to login
                            if (_page.Url.Contains("accounts.google.com"))
                            {
                                _logger.LogInformation("Session expired, need to re-authenticate");
                                throw new InvalidOperationException("Session expired, please run the login activity first");
                            }

                            // Wait for the first email to load with timeout
                            await _page.WaitForSelectorAsync("tr.zA", new() { Timeout = 30000 });
                            
                            // Get first email details
                            var firstEmail = await _page.Locator("tr.zA").First.ElementHandleAsync();
                            if (firstEmail != null)
                            {
                                var subject = await _page.Locator("tr.zA").First.Locator("[data-tooltip-text]").First.TextContentAsync();
                                var sender = await _page.Locator("tr.zA").First.Locator("[email]").First.TextContentAsync();

                                FirstEmailSubject.Set(context, subject ?? "No subject");
                                FirstEmailSender.Set(context, sender ?? "Unknown sender");
                                
                                _logger.LogInformation("Successfully read first email from inbox");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read Gmail inbox");
                throw;
            }
            finally
            {
                await CleanupAsync();
            }

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
