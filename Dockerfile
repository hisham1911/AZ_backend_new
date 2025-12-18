# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY az_backend_new/az_backend_new.csproj ./az_backend_new/
RUN dotnet restore ./az_backend_new/az_backend_new.csproj

# Copy everything else and build
COPY az_backend_new/ ./az_backend_new/
RUN dotnet publish ./az_backend_new/az_backend_new.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

ENTRYPOINT ["dotnet", "az_backend_new.dll"]
