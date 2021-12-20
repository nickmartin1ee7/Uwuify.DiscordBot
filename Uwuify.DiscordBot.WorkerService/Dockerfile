#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Uwuify.DiscordBot.WorkerService/Uwuify.DiscordBot.WorkerService.csproj", "Uwuify.DiscordBot.WorkerService/"]
RUN dotnet restore "Uwuify.DiscordBot.WorkerService/Uwuify.DiscordBot.WorkerService.csproj"
COPY . .
WORKDIR "/src/Uwuify.DiscordBot.WorkerService"
RUN dotnet build "Uwuify.DiscordBot.WorkerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Uwuify.DiscordBot.WorkerService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Uwuify.DiscordBot.WorkerService.dll"]