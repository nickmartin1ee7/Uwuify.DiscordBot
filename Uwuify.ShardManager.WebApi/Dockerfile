#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Uwuify.ShardManager.WebApi/Uwuify.ShardManager.WebApi.csproj", "Uwuify.ShardManager.WebApi/"]
RUN dotnet restore "Uwuify.ShardManager.WebApi/Uwuify.ShardManager.WebApi.csproj"
COPY . .
WORKDIR "/src/Uwuify.ShardManager.WebApi"
RUN dotnet build "Uwuify.ShardManager.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Uwuify.ShardManager.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Uwuify.ShardManager.WebApi.dll"]