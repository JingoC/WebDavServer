# WebDavServer
Implemented as a clean architecture, to replace the FileStorage, replace the [WebDavServer.Infrastructure.FileStorage](https://github.com/JingoC/WebDavServer/tree/master/WebDavServer.Infrastructure.FileStorage) project with your own implementation

## Installation

1. Navigate to the Database folder in the command-line and add the migrations by executing add-migrations.ps1 with an argument of the name you'd like to give it or
    ```dotnet ef migrations add postgres --startup-project ./../WebDavServer.WebApi --project ./../WebDavServer.EF.Postgres.FileStorage -c FileStoragePostgresDbContext -o Migrations```
3. Use docker-compose to start application in docker, you can navigate to http://localhost:5000/swagger for the exposed API end-points.
4. Use any WebDAV client you'd like and connect to http://localhost:5000/, alternatively on Windows, you can map a drive and give it that location. 
