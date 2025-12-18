# Use the official .NET 8 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

# Use the official .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["az_backend_new.csproj", "."]
RUN dotnet restore "./az_backend_new.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "az_backend_new.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "az_backend_new.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "az_backend_new.dll"]