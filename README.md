# Elsa Server and Studio

This is the accompanying source code for the Elsa Server and Studio installation guide.



### Prerequisites PostgreSQL
docker-compose up


### Change Dotnet Core Version
~/.zshrc


dotnet clean
dotnet restore

dotnet dev-certs https --trust
sudo dotnet workload update

dotnet build --configuration Debug

server:
```bash
   cd ElsaServer

   dotnet add package FlaUI.Core
   dotnet add package FlaUI.UIA3

   dotnet add package Microsoft.Playwright
   dotnet tool install --global Microsoft.Playwright.CLI
   playwright install


   dotnet run --project ElsaServer --configuration Debug --urls "http://localhost:5001"

```

### Customizing Elsa Studio
To customize the logo and title:
1. Create a `wwwroot` directory
2. Add your custom logo as `wwwroot/custom-logo.png`
3. Add your custom favicon as `wwwroot/custom-favicon.png`
4. The custom index.html will override the default Elsa Studio interface

## Performance Considerations

*   **Browser Automation (Playwright & FlaUI Activities):**
    *   For activities like `SearchGoogleWithPlaywright`, `ClickTestOnWeb`, `CalculatorAutomationActivity`, and `NotepadAutomationActivity`, performance can be significantly improved by configuring them appropriately.
    *   **Headless Mode:** When using Playwright-based activities (`SearchGoogleWithPlaywright`, `ClickTestOnWeb`), enabling `Headless` mode (which is often the default) is highly recommended for performance-critical workflows. UI rendering adds overhead.
    *   **Timeouts:** Activities interacting with external UIs or websites have configurable timeouts (e.g., `DefaultTimeout` in `SearchGoogleWithPlaywright`). While generous timeouts ensure stability on slow systems or networks, overly long timeouts can cause workflows to appear slow if issues arise. Tune these based on your environment.
    *   **Robust Waits:** The UI automation activities (`CalculatorAutomationActivity`, `NotepadAutomationActivity`) have been updated to use dynamic retry mechanisms instead of fixed delays. This should improve their speed and reliability.
*   **External Factors:**
    *   The performance of activities that interact with external websites (e.g., Google search) or desktop applications (e.g., Calculator, Notepad) is heavily dependent on factors outside the workflow itself. These include network speed, website response times, and the performance of the target application.
    *   Changes to the UI of external websites or applications can also break automation or slow it down. While selectors are chosen to be resilient, this is an inherent risk with UI automation.
*   **Logging:**
    *   While `LogMessageActivity` is lightweight, excessive logging throughout a complex workflow can add I/O overhead. Use logging judiciously for debugging or essential audit trails.
