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
   dotnet add package FlaUI.Core
   dotnet add package FlaUI.UIA3

   dotnet run --project ElsaServer --configuration Debug --urls "http://localhost:5001"

```

### Customizing Elsa Studio
To customize the logo and title:
1. Create a `wwwroot` directory
2. Add your custom logo as `wwwroot/custom-logo.png`
3. Add your custom favicon as `wwwroot/custom-favicon.png`
4. The custom index.html will override the default Elsa Studio interface
