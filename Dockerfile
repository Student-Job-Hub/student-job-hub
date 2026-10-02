FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/StudentJobHub.Api/StudentJobHub.Api.csproj src/StudentJobHub.Api/
RUN dotnet restore src/StudentJobHub.Api/StudentJobHub.Api.csproj

COPY src/StudentJobHub.Api/ src/StudentJobHub.Api/
RUN dotnet publish src/StudentJobHub.Api/StudentJobHub.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 10000
ENTRYPOINT ["sh", "-c", "dotnet StudentJobHub.Api.dll --urls http://0.0.0.0:${PORT:-10000}"]