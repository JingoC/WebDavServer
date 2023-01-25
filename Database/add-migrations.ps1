dotnet ef migrations add $args[0] --startup-project ./../WebDavServer.WebApi --project ./../WebDavServer.EF.Postgres.FileStorage -c FileStoragePostgresDbContext -o Migrations
