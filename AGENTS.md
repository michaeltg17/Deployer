# Deployer

.NET 10 ASP.NET Core minimal API that receives deployment requests and deploys Docker containers via `docker compose`.

## Architecture

- Receives POST `/` with a JSON body containing project, environment, and tag
- Extracts `.env` files from a KeePassXC database using `keepassxc-cli`
- Pulls the image from GHCR via Docker.DotNet, then runs `docker compose up -d --force-recreate` in an isolated temp directory
- Central package management via `Directory.Packages.props`

## Structure

```
├── .dockerignore
├── .gitignore
├── AGENTS.md
├── ci-docker-build.sh              # CI docker build script
├── ci-docker.sh                    # CI docker run script
├── ci.sh                           # CI entrypoint script
├── Deployer.slnx
├── Directory.Build.props           # shared props: net10.0, nullable, implicit usings
├── Directory.Packages.props        # central package versions
├── docker-compose.ci.yml           # CI compose config
├── Dockerfile                      # multi-stage: SDK build → Alpine + docker-cli + keepassxc
├── Dockerfile.ci                   # CI runtime image with test dependencies
├── .github/workflows/ci.yml        # GH Actions: test, build, push to GHCR
├── src/                            # Api project
│   ├── Api.csproj                  # net10.0 Web SDK, Docker.DotNet dep
│   ├── appsettings.json            # minimal defaults (logging, allowed hosts)
│   ├── Program.cs                  # entrypoint, DI, endpoint registration
│   ├── Endpoints/
│   │   └── DeployEndpoint.cs       # POST /: JSON parsing, delegation
│   ├── Exceptions/
│   │   ├── DeployerException.cs    # base exception
│   │   └── InvalidDeployRequestException.cs
│   ├── Extensions/
│   │   ├── ExceptionHandlerExtensions.cs  # problem+json error handler
│   │   └── TypeExtensions.cs              # helper for exception names
│   ├── Logging/
│   │   └── ILoggerExtensions.cs    # source-generated log messages
│   ├── Models/
│   │   ├── DeployRequest.cs        # POCO: project, environment, tag
│   │   └── DeployerSettings.cs     # POCO bound from config
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Services/
│   │   ├── DeploymentService.cs    # validate → extract env → pull image → compose up → cleanup
│   │   ├── IProcessRunner.cs       # interface for process execution (supports stdinInput)
│   │   ├── KeePassEnvService.cs    # keepassxc-cli attachment-export → .env files (password via --no-password + stdin)
│   │   └── ProcessRunner.cs        # real impl: ProcessStartInfo, stdin piping for stdinInput
│   └── Validation/
│       ├── DeployRequestValidator.cs    # request field validation
│       └── DeployerSettingsValidator.cs # IValidateOptions for settings
└── tests/
    ├── Tests.csproj                # xunit.v3, AwesomeAssertions, Docker.DotNet, Mvc.Testing
    ├── TestFixture.cs              # WebApplicationFactory, real Docker client, ProcessRunner
    ├── DeployTests.cs              # 8 tests: validation, missing compose, real deploys
    ├── test.kdbx                   # KeePassXC 2 binary test database
    └── projects/
        └── test-project/
            └── docker-compose.yml  # Deploys ghcr.io/michaeltg17/deployer:${TAG}
```

## Configuration

All config binds from `DeployerSettings` via `builder.Configuration`. Required settings (`ImageRepo`, `KeePassDbPath`, `KeePassDbPassword`) validated at startup via `DeployerSettingsValidator`. `ProjectsDir` is optional, defaults to `/projects`. Application fails to start if any required setting is missing.

Projects are stored under `/projects/<name>/` on disk, each containing a `docker-compose.yml`. Environment secrets (`.env`, `.env.<environment>`) are stored as KeePassXC attachments under `Projects/<name>`.

## Endpoints

| Method | Path      | Description                       |
|--------|-----------|-----------------------------------|
| POST   | `/`       | Triggers deployment               |

`/` expects JSON body: `{ "project": "...", "environment": "...", "tag": "..." }`

Responses use `application/problem+json`. Invalid requests return 400, other errors return 500 with details hidden in production.

## Build & Run

```bash
dotnet run --project src
# or
docker build -t deployer . && docker run --rm -it -v /var/run/docker.sock:/var/run/docker.sock deployer
```

## Tests

```bash
dotnet test tests/Tests.csproj
```

### Test Files

```
└── tests/
    ├── Tests.csproj                # xunit.v3, AwesomeAssertions, Docker.DotNet, Mvc.Testing
    ├── TestFixture.cs              # WebApplicationFactory, real Docker client, ProcessRunner
    ├── DeployTests.cs              # 8 tests: validation, missing compose, real deploys
    ├── test.kdbx                   # KeePassXC 2 binary test database
    └── projects/
        └── test-project/
            └── docker-compose.yml  # Deploys ghcr.io/michaeltg17/deployer:${TAG}
```

### Tests (DeployTests.cs)

8 tests using `TestFixture` — all run against real Docker daemon and real KeePassXC database (`test.kdbx`). Uses AwesomeAssertions for fluent assertions. Validates request parsing, missing fields, missing compose file, and successful deploys.

| Test | Description |
|------|-------------|
| `MissingBody_Returns400` | POST with no body returns 400 |
| `InvalidBody_Returns400` | POST with non-JSON body returns 400 |
| `MissingEnvironment_Returns400` | Missing environment field returns 400 |
| `MissingTag_Returns400` | Missing tag field returns 400 |
| `ValidRequest_NoComposeFile_Returns400` | No compose file returns 400 with error message containing `docker-compose.yml` |
| `ValidRequest_EachEnvironment_Returns400` | Each environment (dev, qa, prod) without compose file returns 400 |
| `ValidRequest_Latest_Returns200_AndStartsContainer` | Deploys `test-project` with `tag: "latest"`, verifies 200 and correct image |
| `ValidRequest_CommitTag_Returns200_AndStartsContainer` | Deploys `test-project` with `tag: "21ec91a"`, verifies 200 and correct image |

`TestFixture` points `TestProjectsDir` at `tests/projects/`, uses real Docker.DotNet client, and uses real `ProcessRunner`. All commands (`keepassxc-cli`, `docker`) execute against the real Docker daemon and real KeePassXC database (`test.kdbx`).

## Coding Conventions

- **No `Async` suffix** — don't name methods `RunAsync`, do `Run`. The `async` modifier on the method body is sufficient.
- **Models over tuples** — use a proper response class instead of `Task<(int, string, string)>`
- **No leading underscore** — name fields `inner`, `client`, `testKdbxPath`, not `_inner`, `_client`, `_testKdbxPath`
