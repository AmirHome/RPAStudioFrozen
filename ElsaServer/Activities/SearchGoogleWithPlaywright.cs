using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Workflows; // For Activity<T> and other core workflow types
using Elsa.Workflows.Attributes; // For [Activity], [Input] attributes
using Elsa.Workflows.Models; // For Input<T>, ActivityExecutionContext
using Microsoft.Playwright;
using Microsoft.Extensions.Logging;

// Define the activity
[Activity("PlaywrightGoogleSearch", "AmirHoss", "Navigates to Google and searches for a specified string using Playwright.")]
public class SearchGoogleWithPlaywright : Activity<string> // Activity<T> where T is the output type (the final URL)
{
    [Input(Description = "The string to search for on Google.")]
    public Input<string?> SearchString { get; set; } = new((string?)null);

    [Input(Description = "Run the browser in headless mode (no UI). Defaults to true.", Category = "Browser Settings", DefaultValue = true)]
    public Input<bool> Headless { get; set; } = new(true);

    [Input(Description = "Default timeout in milliseconds for Playwright operations. Defaults to 60000 (60 seconds).", Category = "Browser Settings", DefaultValue = 60000)]
    public Input<int> DefaultTimeout { get; set; } = new(60000);

    [Input(Description = "The state to wait for after navigation. Defaults to Load.", Category = "Browser Settings", DefaultValue = WaitUntilState.Load)]
    public Input<WaitUntilState> WaitUntil { get; set; } = new(WaitUntilState.Load);

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var searchString = SearchString.Get(context);
        var logger = context.GetRequiredService<ILogger<SearchGoogleWithPlaywright>>();
        context.JournalData.Add("WorkflowStatus", "Starting");

        if (string.IsNullOrWhiteSpace(searchString))
        {
            context.JournalData.Add("Error", "SearchString cannot be empty.");
            context.JournalData.Add("ValidationFailure", "SearchString: The search string cannot be empty.");
            context.JournalData.Add("WorkflowStatus", "Failed");
            context.SetResult("Failed: Search string is empty.");
            return;
        }

        var headless = Headless.Get(context);
        var defaultTimeout = DefaultTimeout.Get(context);
        var waitUntilState = WaitUntil.Get(context);

        logger.LogInformation("Configuration: Headless={Headless}, Timeout={Timeout}ms, SearchString='{SearchString}'", headless, defaultTimeout, searchString);
        context.JournalData.Add("WorkflowStatus", "Configuring Browser");

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
            Timeout = defaultTimeout
        });
        var page = await browser.NewPageAsync();
        page.SetDefaultTimeout(defaultTimeout);

        // Log browser console messages for debugging
        page.Console += (sender, e) => logger.LogInformation("Browser console: {Message}", e.Text);

        try
        {
            logger.LogInformation("Navigating to https://www.google.com");
            context.JournalData.Add("WorkflowStatus", "Navigating to Google");
            await page.GotoAsync("https://www.google.com", new PageGotoOptions { WaitUntil = waitUntilState });

            // Handle Cookie Consent
            logger.LogInformation("Attempting to handle cookie consent");
            context.JournalData.Add("WorkflowStatus", "Handling Cookie Consent");
            try
            {
                var acceptButton = page.Locator("button[id*='L2AGLb'], button[id*='W0wltc'], button[aria-label*='accept' i], button[aria-label*='agree' i]");
                if (await acceptButton.IsVisibleAsync() && await acceptButton.IsEnabledAsync())
                {
                    logger.LogInformation("Clicking consent button");
                    await acceptButton.ClickAsync();
                    await page.WaitForLoadStateAsync(LoadState.Load);
                    if (!headless) await Task.Delay(2000); // Delay for visibility in non-headless mode
                    logger.LogInformation("Cookie consent handled successfully");
                }
                else
                {
                    logger.LogInformation("Cookie consent button not found or not interactable");
                }
            }
            catch (PlaywrightException ex)
            {
                logger.LogWarning("Failed to handle cookie consent: {Message}", ex.Message);
            }

            // Find the search input field
            context.JournalData.Add("WorkflowStatus", "Locating Search Input");
            var searchInput = page.Locator("[name='q'], [role='searchbox']").First;
            if (await searchInput.IsVisibleAsync())
            {
                logger.LogInformation("Search input found. Filling with '{SearchString}'", searchString);
                context.JournalData.Add("WorkflowStatus", "Filling Search Input");
                await searchInput.FillAsync(searchString);
                if (!headless) await Task.Delay(2000); // Delay for visibility
                logger.LogInformation("Pressing Enter to initiate search");
                context.JournalData.Add("WorkflowStatus", "Initiating Search");
                await searchInput.PressAsync("Enter");
                logger.LogInformation("Waiting for search results page");
                context.JournalData.Add("WorkflowStatus", "Waiting for Search Results");
                await page.WaitForURLAsync(url => url.Contains("search?q="), new PageWaitForURLOptions { Timeout = defaultTimeout });
                var finalUrl = page.Url;
                logger.LogInformation("Search completed. Final URL: {FinalUrl}", finalUrl);
                context.JournalData.Add("WorkflowStatus", "Completed");
                context.SetResult(finalUrl);
            }
            else
            {
                logger.LogError("Could not find the search input field on Google");
                context.JournalData.Add("Error", "Could not find the search input field on Google.");
                context.JournalData.Add("PlaywrightFailure", "Could not find the search input field on Google.");
                context.JournalData.Add("WorkflowStatus", "Failed");
                context.SetResult("Failed: Search input not found.");
            }
        }
        catch (PlaywrightException ex)
        {
            logger.LogError(ex, "An error occurred during Playwright operations: {Message}", ex.Message);
            context.JournalData.Add("PlaywrightError", $"An error occurred: {ex.Message}");
            context.JournalData.Add("WorkflowStatus", "Failed");
            context.SetResult($"Error: {ex.Message}");
        }
        finally
        {
            if (!headless)
            {
                logger.LogInformation("Keeping browser open for 5 seconds in non-headless mode");
                await Task.Delay(5000); // Keep browser open for inspection
            }
        }
    }
}