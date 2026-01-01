FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base

# Add Tini - dynamically fetch latest version
RUN apt update \
	&& apt install -y curl jq \
	&& TINI_VERSION=$(curl -s https://api.github.com/repos/krallin/tini/releases/latest | jq -r '.tag_name') \
	&& curl -L https://github.com/krallin/tini/releases/download/${TINI_VERSION}/tini -o /tini \
	&& chmod +x /tini \
	&& apt-get clean \
	&& rm -rf /var/lib/apt/lists/*

WORKDIR /app

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
ENTRYPOINT ["/tini", "--", "dotnet", "Uwuify.DiscordBot.WorkerService.dll"]
