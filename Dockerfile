# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Layer 1: Copy only project files and restore (cached unless deps change)
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY Deployer.slnx ./
COPY src/Api/Api.csproj src/Api/
COPY src/ApiClient/ApiClient.csproj src/ApiClient/
COPY tests/Core.Testing/Core.Testing.csproj tests/Core.Testing/
COPY tests/EndToEndTests/EndToEndTests.csproj tests/EndToEndTests/
COPY tests/IntegrationTests/IntegrationTests.csproj tests/IntegrationTests/
COPY tests/UnitTests/UnitTests.csproj tests/UnitTests/
RUN dotnet restore Deployer.slnx

# Layer 2: Copy full source and publish (invalidated on code change)
COPY src/ src/
COPY tests/ tests/
RUN dotnet publish src/Api/Api.csproj -c Release -o /app

# Stage 2: Runtime (alpine + docker-cli + keepassxc-cli for deployments)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine
RUN apk add --no-cache docker-cli docker-cli-compose keepassxc
WORKDIR /app
COPY --from=build /app .
COPY --from=build /src/tests/Core.Testing/Sandbox /test/sandbox

ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "Api.dll"]