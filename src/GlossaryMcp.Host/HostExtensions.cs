using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using GlossaryMcp.Tools.Extensions;
using GlossaryMcp.Tools.Glossary;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace GlossaryMcp.Host;

public static class HostExtensions
{
    internal static string ServerVersion => Assembly.GetExecutingAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(HostExtensions).Assembly.GetName().Version?.ToString()
        ?? "0.0.0";

    public static IServiceCollection Compose(this IServiceCollection services, IConfiguration configuration)
    {
        var filePath = configuration["file"] ?? "./glossary.jsonl";

        services.AddSingleton(_ => GlossaryStore.Load(filePath));
        services.WithGlossaryMcp();
        services.AddMcpRuntime();

        return services;
    }

    private static IServiceCollection AddMcpRuntime(this IServiceCollection services)
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            WriteIndented = true
        };

        var builder = services.AddMcpServer(options =>
        {
            options.ServerInfo = new Implementation
            {
                Name = "GlossaryMcp",
                Version = ServerVersion
            };
        });

        builder.WithStdioServerTransport();
        builder.WithTools(ServiceExtensions.GetTools(), serializerOptions);

        return services;
    }
}
