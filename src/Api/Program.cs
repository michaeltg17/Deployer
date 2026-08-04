using Api.Endpoints;
using Api.Extensions;
using Api.Models;
using Api.Services;
using Api.Validation;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<DeployerSettings>()
    .BindConfiguration("")
    .ValidateOnStart()
    .Services.AddSingleton<IValidateOptions<DeployerSettings>, DeployerSettingsValidator>()
    .AddSingleton<IDeployerSettings>(sp => sp.GetRequiredService<IOptions<DeployerSettings>>().Value);

builder.Services.AddProblemDetails();
builder.Services.AddSingleton<IProcessRunner, ProcessRunner>();
builder.Services.AddSingleton<KeePassEnvService>();
builder.Services.AddSingleton<DeploymentService>();

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole();

var app = builder.Build();

app.UseCustomExceptionHandler();
DeployEndpoint.Map(app);
TestEndpoints.Map(app);

app.Run();