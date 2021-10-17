FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-server
WORKDIR /src
COPY src/DyndnsUpdater.Server .
RUN dotnet publish DyndnsUpdater.Server.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build-server /app/publish .
ENTRYPOINT dotnet DyndnsUpdater.Server.dll