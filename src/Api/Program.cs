using Api;

DependencyConfigurator.ConfigureValidationWithCamelCase();

WebApplication
    .CreateBuilder(args)
    .AddDependencies()
    .Build()
    .Configure()
    .Run();