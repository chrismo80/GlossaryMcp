using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

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
        .Where(type => type.Implements<Tool>())
        .Distinct();

    private static bool Implements<T>(this Type type) =>
        type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(T));
}

