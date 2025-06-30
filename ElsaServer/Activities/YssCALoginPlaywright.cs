using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace ElsaServer.Activities
{
    [Activity("YssCALoginPlaywright", "AH-Playwright", "Performs a test click and fills login form on Playwright website")]

    public class YssCALoginPlaywright : Activity
    {
        [Input(Description = "YSS sisteminde Takip Bilgileri Giriş", Category = "Browser Settings", DefaultValue = true)]
        public Input<bool> Headless { get; set; } = new(true);

        [Input(Description = "Username for login", Category = "Login Credentials", DefaultValue = "burak_yurtnac")]
        public Input<string> Username { get; set; } = new("");

        [Input(Description = "Password for login", Category = "Login Credentials", DefaultValue = "etcBASE123*")]
        public Input<string> Password { get; set; } = new("");

        [Input(Description = "Captcha text if required", Category = "Login Credentials", DefaultValue = "")]
        public Input<string> Captcha { get; set; } = new("");

        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext? _browserContext;
        private IPage? _page;

        protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
        {
            var headless = Headless.Get(context);
            var username = Username.Get(context);
            var password = Password.Get(context);
            var captcha = Captcha.Get(context);
            var logger = context.GetRequiredService<ILogger<YssCALoginPlaywright>>();
            string url = "http://alacaktakip.ic-a.com.tr/dcs/login.jsp";

            try
            {
                _playwright = await Playwright.CreateAsync();
                var flashChromePath = @"C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
                //var flashChromePath = @"C:\\GoogleChromeForEtcbaseYTS\\GoogleChromeForEtcbaseYTS.exe";
                if (!System.IO.File.Exists(flashChromePath))
                    flashChromePath = @"C:\\GoogleChromeForEtcbaseYTS\\GoogleChromeForEtcbaseYTS.exe";

                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = headless,
                    ExecutablePath = flashChromePath
                });

                _browserContext = await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36",
                    ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
                    ScreenSize = new ScreenSize { Width = 1920, Height = 1080 },
                    Locale = "en-US",
                    TimezoneId = "America/New_York"
                });
                _page = await _browserContext.NewPageAsync();

                await _page.GotoAsync(url);

                // Fill the login form
                await _page.FillAsync("input[name='KullaniciAdi']", username);
                await _page.FillAsync("input[name='Sifre']", password);
                if (!string.IsNullOrEmpty(captcha))
                {
                    await _page.FillAsync("input[name='Dogru']", captcha);
                }

                // Submit the form
                await _page.ClickAsync("button[type='submit']");

                logger.LogInformation("Login form filled and submitted successfully.");
            }
            catch (PlaywrightException ex)
            {
                logger.LogWarning("Failed to login: {Message}", ex.Message);
                throw new Exception($"Failed to visit or fill website {url}", ex);
            }
            finally
            {
                if (!headless && _browser != null)
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