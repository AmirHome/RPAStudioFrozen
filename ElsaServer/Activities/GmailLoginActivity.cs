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

        [Input(Description = "User data directory for persistent browser profile")]
        public Input<string?> UserDataDir { get; set; } = default!;

        [Output(Description = "Login success status")]
        public Output<bool> Success { get; set; } = default!;

        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IPage? _page;
        private IBrowserContext? _browserContext;
        private ILogger<GmailLoginActivity>? _logger;

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            _logger = context.GetRequiredService<ILogger<GmailLoginActivity>>();
            var email = Email.Get(context);
            var password = Password.Get(context);
            var cookieFile = CookieStoragePath.Get(context);
            var maxRetries = MaxRetries.Get(context);
            var userDataDir = UserDataDir.Get(context);
            var retryCount = 0;
            var success = false;

            while (retryCount < maxRetries && !success)
            {
                try
                {
                    retryCount++;
                    _logger.LogInformation("Gmail login attempt {Count} of {Max}", retryCount, maxRetries);

                    // Initialize Playwright with enhanced options
                    _playwright = await Playwright.CreateAsync();
                    if (_playwright != null)
                    {
                        var launchOptions = new BrowserTypeLaunchOptions
                        {
                            Headless = false, // Keep visible for debugging
                            Args = new[]
                            {
                                "--disable-blink-features=AutomationControlled",
                                "--disable-features=VizDisplayCompositor",
                                "--disable-extensions-except=",
                                "--disable-extensions",
                                "--no-sandbox",
                                "--disable-setuid-sandbox",
                                "--disable-dev-shm-usage",
                                "--disable-accelerated-2d-canvas",
                                "--no-first-run",
                                "--no-zygote",
                                "--disable-gpu",
                                "--disable-background-timer-throttling",
                                "--disable-backgrounding-occluded-windows",
                                "--disable-renderer-backgrounding"
                            }
                        };

                        _browser = await _playwright.Chromium.LaunchAsync(launchOptions);

                        var contextOptions = new BrowserNewContextOptions
                        {
                            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36",
                            ViewportSize = new ViewportSize { Width = 1366, Height = 768 },
                            Locale = "en-US",
                            TimezoneId = "America/New_York"
                        };

                        // Use persistent context if user data directory is provided
                        if (!string.IsNullOrEmpty(userDataDir))
                        {
                            _browserContext = await _browser.NewContextAsync(contextOptions);
                        }
                        else
                        {
                            _browserContext = await _browser.NewContextAsync(contextOptions);
                        }

                        _page = await _browserContext.NewPageAsync();

                        if (_page != null)
                        {
                            // Remove automation indicators
                            await _page.AddInitScriptAsync(@"
                                Object.defineProperty(navigator, 'webdriver', {
                                    get: () => undefined,
                                });
                                
                                window.chrome = {
                                    runtime: {},
                                };
                                
                                Object.defineProperty(navigator, 'plugins', {
                                    get: () => [1, 2, 3, 4, 5],
                                });
                                
                                Object.defineProperty(navigator, 'languages', {
                                    get: () => ['en-US', 'en'],
                                });
                            ");

                            // Load existing cookies if available
                            if (File.Exists(cookieFile))
                            {
                                try
                                {
                                    var cookieJson = await File.ReadAllTextAsync(cookieFile);
                                    var cookies = JsonSerializer.Deserialize<Cookie[]>(cookieJson);
                                    if (cookies != null && cookies.Length > 0)
                                    {
                                        await _browserContext.AddCookiesAsync(cookies);
                                        _logger.LogInformation("Loaded existing cookies");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to load existing cookies");
                                }
                            }

                            // Try to navigate to Gmail directly first (in case already logged in)
                            await _page.GotoAsync("https://mail.google.com/mail/u/0/");
                            
                            // Check if already logged in
                            try
                            {
                                await _page.WaitForSelectorAsync("[data-icon='compose']", new() { Timeout = 5000 });
                                success = true;
                                _logger.LogInformation("Already logged in to Gmail");
                            }
                            catch
                            {
                                // Not logged in, proceed with login
                                await PerformLoginAsync(email, password);
                                
                                // Verify login success
                                try 
                                {
                                    await _page.WaitForSelectorAsync("[data-icon='compose']", new() { Timeout = 30000 });
                                    
                                    // Store cookies
                                    if (_browserContext != null)
                                    {
                                        var cookies = await _browserContext.CookiesAsync();
                                        await File.WriteAllTextAsync(cookieFile, JsonSerializer.Serialize(cookies, new JsonSerializerOptions { WriteIndented = true }));
                                        
                                        success = true;
                                        _logger.LogInformation("Successfully logged into Gmail and stored cookies");
                                    }
                                }
                                catch (TimeoutException)
                                {
                                    _logger.LogWarning("Login verification timed out");
                                    
                                    // Check for 2FA or security challenges
                                    var currentUrl = _page.Url;
                                    if (currentUrl.Contains("challenge") || currentUrl.Contains("verify"))
                                    {
                                        _logger.LogWarning("Security challenge detected. Manual intervention may be required.");
                                        // Wait longer for manual intervention
                                        await Task.Delay(60000);
                                    }
                                    throw;
                                }
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
                        var delay = retryCount * 10000; // Exponential backoff
                        _logger.LogInformation("Waiting {Delay}ms before retry", delay);
                        await Task.Delay(delay);
                    }
                }
            }

            await CleanupAsync();
            Success.Set(context, success);
            await context.CompleteActivityAsync();
        }

        private async Task PerformLoginAsync(string email, string password)
        {
            if (_page == null) return;

            // Navigate to Gmail signin with additional parameters
            await _page.GotoAsync("https://accounts.google.com/signin/v2/identifier?service=mail&passive=true&rm=false&continue=https%3A%2F%2Fmail.google.com%2Fmail%2F&ss=1&scc=1&ltmpl=default&ltmplcache=2&emr=1");
            
            // Wait for and enter email
            await _page.WaitForSelectorAsync("input[type='email']", new() { Timeout = 10000 });
            await _page.FillAsync("input[type='email']", email);
            
            // Add human-like delay
            await Task.Delay(Random.Shared.Next(1000, 3000));
            
            await _page.ClickAsync("#identifierNext");
            
            // Wait for password field
            await _page.WaitForSelectorAsync("input[type='password']", new() { Timeout = 15000 });
            
            // Add another human-like delay
            await Task.Delay(Random.Shared.Next(1000, 3000));
            
            await _page.FillAsync("input[type='password']", password);
            
            // Add delay before clicking
            await Task.Delay(Random.Shared.Next(1000, 2000));
            
            await _page.ClickAsync("#passwordNext");
            
            // Wait a bit for processing
            await Task.Delay(3000);
        }

        private async Task CleanupAsync()
        {
            try
            {
                if (_page != null) await _page.CloseAsync();
                if (_browserContext != null) await _browserContext.DisposeAsync();
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