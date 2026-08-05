using Api.Endpoints;
using Api.Models;
using Api.Services;
using Api.Validation;
using FluentValidation;
using Microsoft.Extensions.Options;
using Serilog;
using System.Linq.Expressions;
using System.Reflection;

namespace Api
{
    public static class DependencyConfigurator
    {
        public static WebApplicationBuilder AddDependencies(this WebApplicationBuilder builder)
        {
            builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = true);

            builder.AddSerilog();

            builder.Services
                .AddAppDependencies()
                .AddSettings()
                .AddProblemDetails();

            return builder;
        }

        public static IServiceCollection AddAppDependencies(this IServiceCollection services)
        {
            return services
                .AddSingleton<IProcessRunner, ProcessRunner>()
                .AddSingleton<KeePassEnvService>()
                .AddSingleton<DeploymentService>();
        }

        public static IServiceCollection AddSettings(this IServiceCollection services)
        {
            return services
                .AddOptions<DeployerSettings>()
                .BindConfiguration("")
                .ValidateOnStart()
                .Services.AddSingleton<IValidateOptions<DeployerSettings>, DeployerSettingsValidator>()
                .AddSingleton<IDeployerSettings>(sp => sp.GetRequiredService<IOptions<DeployerSettings>>().Value);
        }

        public static WebApplicationBuilder AddSerilog(this WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((context, services, configuration) =>
            {
                ApplyCommonSerilogConfiguration(context, services, configuration);
                configuration.WriteTo.Console();
            });

            return builder;
        }

        public static WebApplication MapEndpoints(this WebApplication app)
        {
            DeployEndpoint.Map(app);
            TestEndpoints.Map(app.MapGroup("/test"));
            return app;
        }

        public static void ApplyCommonSerilogConfiguration(
            HostBuilderContext context, IServiceProvider services, LoggerConfiguration configuration)
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext();
        }

        public static void ConfigureValidationWithCamelCase()
        {
            var defaultResolver = ValidatorOptions.Global.PropertyNameResolver;

            string camelCaseResolver(Type type, MemberInfo memberInfo, LambdaExpression expression)
            {
                var pascal = defaultResolver(type, memberInfo, expression);
                return string.Join(ValidatorOptions.Global.PropertyChainSeparator,
                    pascal.Split(ValidatorOptions.Global.PropertyChainSeparator, StringSplitOptions.None)
                        .Select(p => char.ToLowerInvariant(p[0]) + p[1..]));
            }

            ValidatorOptions.Global.PropertyNameResolver = camelCaseResolver;
            ValidatorOptions.Global.DisplayNameResolver = camelCaseResolver;
        }

        public static WebApplication Configure(this WebApplication app)
        {
            //Exception middleware first to catch exceptions
            app.UseExceptionHandler().UseStatusCodePages();

            app.MapEndpoints();

            return app;
        }
    }
}
