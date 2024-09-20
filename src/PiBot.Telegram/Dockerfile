FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["PiBot.Telegram/PiBot.Telegram.csproj", "PiBot.Telegram/"]
COPY ["PiBot.Utils/PiBot.Utils.csproj", "PiBot.Utils/"]
COPY ["PiBot.Core/PiBot.Core.csproj", "PiBot.Core/"]
RUN dotnet restore "PiBot.Telegram/PiBot.Telegram.csproj"
COPY "PiBot.Telegram" "PiBot.Telegram"
COPY "PiBot.Utils" "PiBot.Utils"
COPY "PiBot.Core" "PiBot.Core"
WORKDIR "/src/PiBot.Telegram"
RUN dotnet build "PiBot.Telegram.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "PiBot.Telegram.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8443
ENTRYPOINT ["dotnet", "PiBot.Telegram.dll"]
