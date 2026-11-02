FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore Logistics.Api.csproj && dotnet publish Logistics.Api.csproj -c Release -o /app --no-restore
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:5000
# Supply JWT_SECRET and persistent DB volume before running. SQLite file path uses working directory.
ENTRYPOINT ["dotnet","Logistics.Api.dll"]
