FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-client
WORKDIR /src
COPY src/DyndnsUpdater.Client .
RUN dotnet publish DyndnsUpdater.Client.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build-client /app/publish .
ENV DYNDNS_CHECKINTERVAL=60000
ENV DYNDNS_DNSIP=8.8.8.8
ENV DYNDNS_PROVIDER=cloudflare
ENV DYNDNS_UPDATERURL=
ENV DYNDNS_CLOUDFLARE_ZONE=
ENV DYNDNS_CLOUDFLARE_RECORD=
ENV DYNDNS_CLOUDFLARE_TOKEN=
ENTRYPOINT dotnet DyndnsUpdater.Client.dll