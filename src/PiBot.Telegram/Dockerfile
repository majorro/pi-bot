# TODO: remove ConfigurationPlaceholders/pull/7
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
#FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PiBot.Telegram/PiBot.Telegram.csproj", "PiBot.Telegram/"]
COPY ["PiBot.Data/PiBot.Data.csproj", "PiBot.Data/"]
COPY ["PiBot.Core/PiBot.Core.csproj", "PiBot.Core/"]

RUN dotnet restore "PiBot.Telegram/PiBot.Telegram.csproj"
COPY "PiBot.Telegram" "PiBot.Telegram"
COPY "PiBot.Data" "PiBot.Data"
COPY "PiBot.Core" "PiBot.Core"
WORKDIR "/src/PiBot.Telegram"
RUN dotnet build "PiBot.Telegram.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PiBot.Telegram.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "PiBot.Telegram.dll"]
