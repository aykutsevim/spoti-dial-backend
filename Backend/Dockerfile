# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY SpotiDialBackend.csproj .
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet publish SpotiDialBackend.csproj -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Note: Running as root due to LXC/sysctl restrictions
# In production, consider using Kubernetes security contexts or other methods

ENTRYPOINT ["dotnet", "SpotiDialBackend.dll"]
