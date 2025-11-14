# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution file
COPY Ai-Company.sln .

# Copy project files
COPY Domain/Domain.csproj Domain/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY Application/Application.csproj Application/
COPY Ai-Company/Ai-Company.csproj Ai-Company/

# Restore dependencies
RUN dotnet restore Ai-Company.sln

# Copy all source code
COPY Domain/ Domain/
COPY Infrastructure/ Infrastructure/
COPY Application/ Application/
COPY Ai-Company/ Ai-Company/

# Build the application
WORKDIR /src/Ai-Company
RUN dotnet build -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks (optional)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Copy Firebase credentials file (if exists)
COPY --from=build /src/Ai-Company/ai-company-configure-firebase-adminsdk-fbsvc-7037480c2a.json ./

# Expose port
EXPOSE 8080
EXPOSE 8081

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check (using root endpoint)
HEALTHCHECK --interval=30s --timeout=3s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/ || exit 1

# Run the application
ENTRYPOINT ["dotnet", "Ai-Company.dll"]
