FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY global.json FoodHub.sln ./
COPY src/FoodHub.Domain/FoodHub.Domain.csproj src/FoodHub.Domain/
COPY src/FoodHub.Application/FoodHub.Application.csproj src/FoodHub.Application/
COPY src/FoodHub.Infrastructure/FoodHub.Infrastructure.csproj src/FoodHub.Infrastructure/
COPY src/FoodHub.API/FoodHub.API.csproj src/FoodHub.API/
RUN dotnet restore src/FoodHub.API/FoodHub.API.csproj

COPY src/ src/
RUN dotnet publish src/FoodHub.API/FoodHub.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "FoodHub.API.dll"]
