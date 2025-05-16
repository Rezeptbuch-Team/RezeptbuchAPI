# Build-Image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Runtime-Image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
EXPOSE 5112
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RezeptbuchAPI.dll"]