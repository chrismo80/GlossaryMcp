using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace GlossaryMcp.Tools.Extensions;

public static class ServiceExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection WithGlossaryMcp() => services
            .AddTools();

        private IServiceCollection AddTools()
        {
            foreach (var type in GetTools())
                services.AddSingleton(type);

            return services;
        }
    }

    public static IEnumerable<Type> GetTools() => Assembly.GetExecutingAssembly()
        .GetTypes()
        .Where(IsTool)
        .Distinct();

    private static bool IsTool(Type type) =>
        type is { IsClass: true, IsAbstract: false } &&
        type.GetCustomAttribute<McpServerToolTypeAttribute>(false) is not null;
}