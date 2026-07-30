# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["LazyTravel/LazyTravel.csproj", "LazyTravel/"]
RUN dotnet restore "LazyTravel/LazyTravel.csproj"

COPY . .
RUN dotnet build "LazyTravel/LazyTravel.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LazyTravel/LazyTravel.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5000 5001
ENV ASPNETCORE_URLS=http://+:5000;https://+:5001
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "LazyTravel.dll"]
