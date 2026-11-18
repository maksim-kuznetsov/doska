# Сборка и публикация серверного API.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY backend/Doska.sln backend/
COPY backend/src backend/src
COPY backend/tests backend/tests

RUN dotnet publish backend/src/Doska.Api/Doska.Api.csproj -c Release -o /app/publish --nologo

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Doska.Api.dll"]