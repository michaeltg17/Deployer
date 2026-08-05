# Deployer

.NET 10 ASP.NET Core minimal API that receives deployment requests and deploys Docker containers via `docker compose`.

## Architecture

- Receives POST `/` with a JSON body containing project, environment, and tag
- Extracts `.env` files from a KeePassXC database using `keepassxc-cli`
- Pulls the image from GHCR via Docker.DotNet, then runs `docker compose up -d --force-recreate` in an isolated temp directory
- Logging via Serilog with scoped enrichment
- Central package management via `Directory.Packages.props`
- Dependency injection, middleware, and endpoint mapping centralized in `DependencyConfigurator`

## Structure

```
├── .dockerignore
├── .editorconfig
├── .gitattributes
├── .github/workflows/ci.yml        # GH Actions: test, build, push to GHCR
├── .gitignore
├── AGENTS.md
├── ci-docker.sh                    # CI docker build+run script
├── ci.sh                           # CI entrypoint script
├── Deployer.slnx
├── Directory.Build.props           # shared props: net10.0, nullable, implicit usings, central pkgs, code analysis
├── Directory.Packages.props        # central package versions
├── Dockerfile                      # multi-stage: SDK build → Alpine + docker-cli + keepassxc
├── Dockerfile.ci                   # CI runtime image with test dependencies
├── README.md
├── src/
│   ├── Api/                        # main API project (Web SDK)
│   │   ├── Api.csproj              # FluentValidation, Serilog deps
│   │   ├── appsettings.json        # minimal defaults (logging, allowed hosts)
│   │   ├── Program.cs              # entrypoint: DependencyConfigurator setup → build → configure → run
│   │   ├── DependencyConfigurator.cs  # DI, Serilog, settings, endpoint mapping, middleware, validation config
│   │   ├── Builders/
│   │   │   ├── Builder.cs          # abstract builder base (IBuilder impl)
│   │   │   ├── BuilderWithInstance.cs  # builder with TEntity instance
│   │   │   ├── BuilderWithValues.cs    # builder with WithValues action
│   │   │   └── IBuilder.cs           # IBuilder<T> interface
│   │   ├── Endpoints/
│   │   │   ├── DeployEndpoint.cs       # POST /: JSON parsing, delegation
│   │   │   └── TestEndpoints.cs        # /test/* test endpoints (GetOk, Post, ThrowInternalServerError)
│   │   ├── Exceptions/
│   │   │   ├── DeployerException.cs    # base exception
│   │   │   ├── InvalidDeployRequestException.cs
│   │   │   └── NoSecretsFoundException.cs
│   │   ├── Extensions/
│   │   │   ├── ExceptionHandlerExtensions.cs  # problem+json error handler, traceId, exception detail
│   │   │   ├── TypeExtensions.cs              # helper for exception names
│   │   │   └── ValidationFailureExtensions.cs  # FluentValidation failures → ProblemDetails errors
│   │   ├── Models/
│   │   │   ├── DeployerSettings.cs     # POCO bound from config, implements IDeployerSettings
│   │   │   ├── IDeployerSettings.cs    # interface for settings
│   │   │   ├── DeployRequest.cs        # record POCO: project, environment, tag
│   │   │   └── ProcessResult.cs        # POCO: exit code, stdout, stderr
│   │   ├── Properties/
│   │   │   └── launchSettings.json
│   │   ├── Services/
│   │   │   ├── DeploymentService.cs    # validate → extract env → pull image → compose up → cleanup
│   │   │   ├── KeePassEnvService.cs    # keepassxc-cli attachment-export → .env files (password via stdin)
│   │   │   └── ProcessRunner.cs        # process execution: ProcessStartInfo, stdin piping, env vars
│   │   └── Validation/
│   │       ├── DeployRequestValidator.cs    # request field validation
│   │       └── DeployerSettingsValidator.cs # IValidateOptions for settings
│   ├── ApiClient/                  # HTTP client library for testing the API
│   │   ├── ApiClient.cs            # HttpClient wrapper: Deploy(), Test endpoints
│   │   ├── ApiClient.csproj        # refs Api project, AspNetCore.App framework
│   │   ├── Converters/
│   │   │   └── NestedObjectConverter.cs  # JSON converter for nested/object types (ExpandoObject, numbers)
│   │   ├── Endpoints/
│   │   │   └── TestEndpoints.cs        # client methods: Post, ThrowInternalServerError, GetOk, RequestUnexistingRoute
│   │   ├── Exceptions/
│   │   │   ├── ApiClientException.cs   # base client exception
│   │   │   └── ApiException.cs         # wraps ProblemDetails from error responses
│   │   ├── Extensions/
│   │   │   ├── HttpResponseMessageExtensions.cs  # To<T>() extension: deserialize, validate ProblemDetails
│   │   │   └── ProblemDetailsExtensions.cs  # ToJsonString() serialization
│   │   └── Validators/
│   │       └── ProblemDetailsValidator.cs  # FluentValidation for ProblemDetails (type, title, status)
│   └── Core/                       # shared infrastructure (no external deps)
│       ├── Core.csproj             # bare net10.0 library
│       └── Builders/
│           ├── Builder.cs              # abstract builder base (IBuilder impl)
│           ├── BuilderWithInstance.cs  # builder with TEntity instance
│           ├── BuilderWithValues.cs    # builder with WithValues action
│           └── IBuilder.cs             # IBuilder<T> interface
└── tests/
    ├── Core.Testing/                 # shared testing utilities
    │   ├── Core.Testing.csproj       # refs Api + ApiClient, AwesomeAssertions
    │   ├── Builders/
    │   │   └── ProblemDetailsBuilder.cs  # fluent builder for expected ProblemDetails
    │   ├── Extensions/
    │   │   └── ProblemDetailsExtensions.cs  # scoped extensions: TraceId, Exception properties
    │   ├── Sandbox/
    │   │   ├── projects/
    │   │   │   ├── test-project/
    │   │   │   │   ├── docker-compose.yml   # deploys ghcr.io/michaeltg17/deployer:${TAG}
    │   │   │   │   └── docker-compose.dev.yml
    │   │   │   └── no-secrets/
    │   │   │       └── docker-compose.yml   # compose with no matching KeePassXC entries
    │   │   └── secrets.kdbx              # KeePassXC test database
    │   └── Validators/
    │       ├── ExceptionValidator.cs     # validates exception text format (type, stack, source)
    │       ├── ProblemDetailsValidator.cs # validates HTTP error responses against expected ProblemDetails
    │       └── TraceIdValidator.cs       # validates W3C traceId format
    ├── IntegrationTests/             # integration tests: WebApplicationFactory, real Docker
    │   ├── IntegrationTests.csproj   # xunit.v3, xunit.DependencyInjection, Serilog.Sinks.XUnit.Injectable
    │   ├── Startup.cs                # xunit.DependencyInjection: register DI, BeforeAfterTest, TestFixture
    │   ├── BeforeAfterTestConfiguration.cs  # per-test DI injection, Test initialization
    │   ├── Test.cs                   # abstract base: ApiClient, DockerClient, IAsyncLifetime
    │   ├── TestFixture.cs            # WebApplicationFactory, Serilog sink, config overrides, container cleanup
    │   ├── TestCollectionFixture.cs  # collection definition for shared TestFixture
    │   ├── TestHelper.cs             # shared container stop/remove, list helpers
    │   ├── TestStartupFilter.cs      # IStartupFilter: adds HttpLogging middleware
    │   ├── xunit.runner.json         # parallelism: 1 thread
    │   └── Tests/
    │       ├── ApiBehaviourTests/
    │       │   ├── BadRequestTests.cs              # parameter binding failures → problem+json
    │       │   ├── CommonApiBehaviourTests.cs       # route 404, successful POST
    │       │   └── DevelopmentApiBehaviourTests.cs  # 500 exposes exception in dev
    │       ├── ApiClient/
    │       │   └── ApiClientTests.cs               # ApiException on error, ApiClientException on no content
    │       └── DeployTests.cs                      # 7 tests: missing fields, missing compose, real deploys
    └── EndToEndTests/                # end-to-end: builds Docker image, runs container, hits API
        ├── EndToEndTests.csproj      # xunit.v3, Docker.DotNet, no Mvc.Testing
        ├── EndToEndFixture.cs        # IAsyncLifetime: builds image, starts container, health check
        ├── EndToEndDeployTests.cs    # 3 tests: latest, commit tag, no-secrets (against running container)
        └── TestHelper.cs             # shared container stop/remove, list helpers
```

## Configuration

All config binds from `DeployerSettings` via `builder.Configuration`. Required settings (`KeePassDbPath`, `KeePassDbPassword`) validated at startup via `DeployerSettingsValidator`. `ProjectsDir` defaults to `/projects`. `KeePassDbPath` defaults to `secrets.kdbx`. `ThrowIfNoSecrets` defaults to `true` — when enabled, deployment fails with `NoSecretsFoundException` if no environment variables are extracted from KeePassXC for the requested project/environment. Application fails to start if any required setting is missing.

FluentValidation is configured with camelCase property names via `DependencyConfigurator.ConfigureValidationWithCamelCase()`. Exception handling uses `application/problem+json` via `DependencyConfigurator.UseExceptionHandler()` with traceId injection and exception detail in development mode.

`DependencyConfigurator` centralizes: DI registration (`AddAppDependencies`, `AddSettings`), Serilog setup, endpoint mapping, middleware pipeline configuration, and FluentValidation camelCase naming.

Projects are stored under `/projects/<name>/` on disk, each containing a `docker-compose.yml`. Environment secrets (`.env`, `.env.<environment>`) are stored as KeePassXC attachments under `Projects/<name>`.

## Endpoints

| Method | Path      | Description                       |
|--------|-----------|-----------------------------------|
| POST   | `/`       | Triggers deployment               |

`/` expects JSON body: `{ "project": "...", "environment": "...", "tag": "..." }`

Responses use `application/problem+json`. Invalid requests return 400, other errors return 500 with details hidden in production.

Test endpoints (also used for integration testing):

| Method | Path                        | Description                    |
|--------|-----------------------------|--------------------------------|
| GET    | `/test/GetOk`               | Returns 200 OK                 |
| POST   | `/test/Post/{id}`           | Accepts id, date (query), body |
| POST   | `/test/ThrowInternalServerError` | Throws exception for 500 testing |

## Build & Run

```bash
dotnet run --project src/Api
# or
docker build -t deployer . && docker run --rm -it -v /var/run/docker.sock:/var/run/docker.sock deployer
```

## Tests

### Integration Tests

Run against `WebApplicationFactory<Program>` with real Docker daemon and real KeePassXC database (`secrets.kdbx`). Uses `xunit.DependencyInjection` for DI, `Serilog.Sinks.XUnit.Injectable` for log capture, `ApiClient` for HTTP calls. `TestFixture` sets `ThrowIfNoSecrets=false`. Parallelism limited to 1 thread.

```bash
dotnet test tests/IntegrationTests/IntegrationTests.csproj
```

- **DeployTests.cs** — 7 tests: missing project/environment/tag (400), missing compose file (400), each environment (400), and 2 successful deploys (latest, commit tag)
- **ApiBehaviourTests/** — common API behavior (404, 200), bad request parameter binding (theory with 5 cases), development 500 with exception exposure
- **ApiClientTests.cs** — ApiException with ProblemDetails on error responses, ApiClientException on no-content

### End-to-End Tests

Build Docker image from `Dockerfile`, start container with mounted test data, and call the running API. Verifies deployment against the actual containerized application.

```bash
dotnet test tests/EndToEndTests/EndToEndTests.csproj
```

- **EndToEndDeployTests.cs** — 3 tests: latest tag, commit tag, no-secrets with setting disabled

### Core Testing

Shared testing library referenced by both IntegrationTests and EndToEndTests. Provides `ProblemDetailsBuilder` for expected response construction, validators for traceId/exception/ProblemDetails format, scoped extensions on ProblemDetails, and sandbox test data (compose files, KeePassXC database).

## Coding Conventions

- **No `Async` suffix** — don't name methods `RunAsync`, do `Run`. The `async` modifier on the method body is sufficient.
- **Models over tuples** — use a proper response class instead of `Task<(int, string, string)>`
- **No leading underscore** — name fields `inner`, `client`, `testKdbxPath`, not `_inner`, `_client`, `_testKdbxPath`
