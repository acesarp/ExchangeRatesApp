# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["ExchangeRates.Server/ExchangeRates.Server.csproj", "ExchangeRates.Server/"]
COPY ["ExchangeRates.Domain/ExchangeRates.Domain.csproj", "ExchangeRates.Domain/"]
COPY ["ExchangeRates.Infrastructure/ExchangeRates.Infrastructure.csproj", "ExchangeRates.Infrastructure/"]

RUN dotnet restore "./ExchangeRates.Server.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./ExchangeRates.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./ExchangeRates.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ExchangeRates.Server.dll"]