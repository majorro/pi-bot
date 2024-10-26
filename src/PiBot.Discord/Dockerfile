# TODO: fix when https://github.com/DaveSenn/ConfigurationPlaceholders/pull/7 will be merged
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
#FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PiBot.Discord/PiBot.Discord.csproj", "PiBot.Discord/"]
COPY ["PiBot.Data/PiBot.Data.csproj", "PiBot.Data/"]
COPY ["PiBot.Core/PiBot.Core.csproj", "PiBot.Core/"]
# TODO: remove when available on nuget.org
RUN dotnet nuget add source https://pkgs.dev.azure.com/tgbots/Telegram.Bot/_packaging/release/nuget/v3/index.json
RUN dotnet restore "PiBot.Discord/PiBot.Discord.csproj"
COPY "PiBot.Discord" "PiBot.Discord"
COPY "PiBot.Data" "PiBot.Data"
COPY "PiBot.Core" "PiBot.Core"
WORKDIR "/src/PiBot.Discord"
RUN dotnet build "PiBot.Discord.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PiBot.Discord.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PiBot.Discord.dll"]
