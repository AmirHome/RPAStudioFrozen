## Install 
   https://docs.elsaworkflows.io/application-types/elsa-server-+-studio-wasm



## Change Dotnet Core Version to 9.0
~/.zshrc
dotnet --version

## Ready
```bash

    dotnet clean
    dotnet restore
    dotnet dev-certs https --trust
    sudo dotnet workload update

    dotnet build --configuration Debug

```

Get-Acl .\ElsaServer\elsa.sqlite.db | Format-List

## Run
```bash
    cd ElsaServer

    dotnet add package FlaUI.Core
    dotnet add package FlaUI.UIA3

    dotnet add package Microsoft.Playwright
    dotnet tool install --global Microsoft.Playwright.CLI
    playwright install

    dotnet run --project ElsaServer --configuration Debug --urls "http://localhost:5001"
```