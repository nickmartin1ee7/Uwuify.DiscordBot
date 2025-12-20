FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app
RUN apt update \
	&& apt install -y curl

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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
