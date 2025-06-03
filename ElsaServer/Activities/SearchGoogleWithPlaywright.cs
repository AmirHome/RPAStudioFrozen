using System.Threading.Tasks;
using Elsa.Extensions;
using Elsa.Workflows; // For Activity<T> and other core workflow types
using Elsa.Workflows.Attributes; // For [Activity], [Input] attributes
using Elsa.Workflows.Models; // For Input<T>, ActivityExecutionContext
using Microsoft.Playwright;

// Define the activity
[Activity("Web", "AmirHoss", "Navigates to Google and searches for a specified string using Playwright.")]
public class SearchGoogleWithPlaywright : Activity<string> // Activity<T> where T is the output type (the final URL)
{
    // Define input properties
    // [Input(Description = "The string to search for on Google.")]
    // public Input<string?> SearchString { get; set; } = new("");
    public Input<string> SearchString { get; set; } = default!;

    // [Input(Description = "Run the browser in headless mode (no UI). Defaults to true.", Category = "Browser Settings")]
    public Input<bool> Headless { get; set; } = new(true);

    // [Input(Description = "Default timeout in milliseconds for Playwright operations. Defaults to 30000 (30 seconds).", Category = "Browser Settings")]
    public Input<int> DefaultTimeout { get; set; } = new(30000); // 30 seconds

    // Define the execution logic
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var searchString = SearchString.Get(context);

        if (string.IsNullOrWhiteSpace(searchString))
        {
            // Add a validation error if the search string is empty
            context.JournalData.Add("Error", "SearchString cannot be empty.");
            context.JournalData.Add("ValidationFailure", "SearchString: The search string cannot be empty.");
            // Indicate failure by setting a result or returning
            context.SetResult("Failed: Search string is empty.");
            return;
        }

        var headless = Headless.Get(context);
        var defaultTimeout = DefaultTimeout.Get(context);
        context.JournalData.Add("Configuration", $"Headless: {headless}, Timeout: {defaultTimeout}ms, Search String: '{searchString}'");

        // Use Playwright
        // Playwright.CreateAsync() downloads the necessary browser binaries if not already present
        using var playwright = await Playwright.CreateAsync();
        // Launch a browser instance (Chromium, Firefox, or Webkit)
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = headless,
            Timeout = defaultTimeout // Applies to browser launch
        });
        // Create a new page (tab) in the browser
        var page = await browser.NewPageAsync();
        page.SetDefaultTimeout(defaultTimeout); // Set default timeout for page operations

        try
        {
            // Navigate to Google
            context.JournalData.Add("Navigation", "Navigating to https://www.google.com");
            await page.GotoAsync("https://www.google.com");

            // --- Handle Cookie Consent (Common Issue) ---
            // Google often shows a cookie consent dialog. This part is highly dependent
            // on Google's current UI and locale. The selector below is an example
            // and might need adjustment. A more robust solution might involve
            // checking for the dialog's presence before attempting to click, or using multiple selectors.
            try
            {
                 // Example: Try to find and click an "Accept all" button
                 // Look for a button containing the text "Accept all" (case-insensitive)
                 var acceptButton = page.Locator("button:has-text('Accept all', ignore-case=true)");
                 // Check if the button is visible and enabled before clicking
                 if (await acceptButton.IsVisibleAsync() && await acceptButton.IsEnabledAsync())
                 {
                     context.JournalData.Add("CookieConsent", "Attempting to click 'Accept all' button for cookies.");
                     await acceptButton.ClickAsync();
                     // Wait for the page to settle after accepting cookies
                     await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                     context.JournalData.Add("CookieConsent", "'Accept all' button clicked and page settled.");
                 }
                 else
                 {
                    context.JournalData.Add("CookieConsent", "Cookie consent 'Accept all' button not found or not interactable. Proceeding...");
                 }
            }
            catch (PlaywrightException ex)
            {
                 // Log or handle cases where the cookie dialog interaction fails
                 context.JournalData.Add("CookieConsentError", $"Failed to handle cookie consent: {ex.Message}");
                 // Continue, as the main functionality might still work if the dialog was not critical or already dismissed.
            }
            // --- End Cookie Consent Handling ---

            // Find the search input field
            // Google's search input often has the name 'q' or role 'searchbox'.
            // We use .First to select the first matching element if multiple exist.
            var searchInput = page.Locator("[name='q'], [role='searchbox']").First;

            // Check if the search input is visible before interacting
            if (await searchInput.IsVisibleAsync())
            {
                 context.JournalData.Add("SearchAction_InputFound", "Search input found. Filling search string.");
                 // Type the search string into the input field
                 await searchInput.FillAsync(searchString);

                 // Press Enter to perform the search
                 context.JournalData.Add("SearchAction_PressEnter", "Pressing Enter to initiate search.");
                 await searchInput.PressAsync("Enter");

                 // Wait for navigation to complete after pressing Enter
                 // Use LoadState.NetworkIdle to wait until there are no network connections for 500ms
                 await page.WaitForNavigationAsync(new PageWaitForNavigationOptions { WaitUntil = WaitUntilState.NetworkIdle });
                 
                 var finalUrl = page.Url;
                 context.JournalData.Add("SearchSuccess", $"Search completed. Final URL: {finalUrl}");
                 // Set the output of the activity to the final URL (the search results page)
                 context.SetResult(finalUrl);
            }
            else
            {
                 // If the search input wasn't found, add a validation error and set a failure result
                 context.JournalData.Add("Error", "Could not find the search input field on Google.");
                 context.JournalData.Add("PlaywrightFailure", "Could not find the search input field on Google.");
                 context.SetResult("Failed: Search input not found.");
            }
        }
        catch (PlaywrightException ex)
        {
            // Catch any Playwright-specific errors during execution
            context.JournalData.Add("PlaywrightError", $"An error occurred: {ex.Message} (Stack: {ex.StackTrace})");
            // For Elsa 3+, you might set a specific outcome or just rely on the journal and result for error indication.
            context.SetResult($"Error: {ex.Message}"); // Set a result indicating failure
        }
    }
}