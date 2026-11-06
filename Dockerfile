FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

COPY UCTWebGame/UCTWebGame.csproj UCTWebGame/
RUN dotnet restore UCTWebGame/UCTWebGame.csproj

COPY UCTWebGame/ UCTWebGame/
WORKDIR /source/UCTWebGame
RUN dotnet publish UCTWebGame.csproj --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000
ENTRYPOINT ["dotnet", "UCTWebGame.dll"]
