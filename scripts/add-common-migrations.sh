#!/bin/bash

MIGRATION_NAME="RemoveSoftDeletion"

cd ../
dotnet ef migrations add --project src/PiBot.Data/PiBot.Data.csproj --startup-project src/PiBot.Telegram/PiBot.Telegram.csproj --context PiBot.Data.Telegram.TelegramPgContext --configuration Debug $MIGRATION_NAME --output-dir Telegram/Migrations
dotnet ef migrations add --project src/PiBot.Data/PiBot.Data.csproj --startup-project src/PiBot.Discord/PiBot.Discord.csproj --context PiBot.Data.Discord.DiscordPgContext --configuration Debug $MIGRATION_NAME --output-dir Discord/Migrations
dotnet ef migrations add --project src/PiBot.Data/PiBot.Data.csproj --startup-project src/PiBot.Telegram/PiBot.Telegram.csproj --context PiBot.Data.Readonly.ReadonlyPgContext --configuration Debug $MIGRATION_NAME --output-dir Readonly/Migrations