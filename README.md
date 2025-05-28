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

server:dotnet run --configuration Debug --urls "http://localhost:5001"
studio:dotnet run --configuration Debug --urls "http://localhost:5000"