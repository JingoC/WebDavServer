#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
ENV ASPNETCORE_ENVIRONMENT=Staging

WORKDIR /src
COPY ["WebDavServer.WebApi/WebDavServer.WebApi.csproj", "WebDavServer.WebApi/"]
COPY ["WebDavServer.Infrastructure/WebDavServer.Infrastructure.csproj", "WebDavServer.Infrastructure/"]
COPY ["WebDavServer.Infrastructure.FileStorage/WebDavServer.Infrastructure.FileStorage.csproj", "WebDavServer.Infrastructure.FileStorage/"]
COPY ["WebDavService.Application/WebDavService.Application.csproj", "WebDavService.Application/"]
COPY ["WebDavServer.Infrastructure.WebDav/WebDavServer.Infrastructure.WebDav.csproj", "WebDavServer.Infrastructure.WebDav/"]
COPY ["WebDavServer.Infrastructure.Cache/WebDavServer.Infrastructure.Cache.csproj", "WebDavServer.Infrastructure.Cache/"]
RUN dotnet restore "WebDavServer.WebApi/WebDavServer.WebApi.csproj"
COPY . .
WORKDIR "/src/WebDavServer.WebApi"
RUN dotnet build "WebDavServer.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
ENV ASPNETCORE_ENVIRONMENT=Staging
RUN dotnet publish "WebDavServer.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
ENV ASPNETCORE_ENVIRONMENT=Staging
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebDavServer.WebApi.dll"]
