FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BookBoard.csproj ./
RUN dotnet restore "./BookBoard.csproj"

COPY . .
RUN dotnet publish "./BookBoard.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# Persistent SQLite/data-protection fallback when Postgres is not configured.
# Attach a Render disk (or Docker volume) at /data, or set DATABASE_URL to a
# Postgres instance so accounts, boards, and saved inspiration survive deploys.
RUN mkdir -p /data
VOLUME ["/data"]
ENV BOOKBOARD_DATA_DIR=/data

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false

EXPOSE 10000

ENTRYPOINT ["dotnet", "BookBoard.dll"]
