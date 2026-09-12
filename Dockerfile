# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS restore
WORKDIR /src

COPY ["LazyTravel.Shared/LazyTravel.Shared.csproj", "LazyTravel.Shared/"]
COPY ["tours-api/tours-api.csproj", "tours-api/"]
RUN dotnet restore "tours-api/tours-api.csproj"

FROM restore AS publish
COPY . .
RUN dotnet publish "tours-api/tours-api.csproj" \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DataProtection__KeyPath=/var/lib/lazytravel/keys
EXPOSE 8080

RUN mkdir -p /app/wwwroot/uploads /var/lib/lazytravel/keys \
    && chown -R "$APP_UID:$APP_UID" /app /var/lib/lazytravel
USER $APP_UID

ENTRYPOINT ["dotnet", "tours-api.dll"]
