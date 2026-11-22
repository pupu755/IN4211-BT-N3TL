using System.Reflection;
using FoodHub.Application;
using FoodHub.Domain;
using FoodHub.Infrastructure;

namespace FoodHub.Tests.Architecture;

public class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(DomainAssemblyMarker).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(ApplicationAssemblyMarker).Assembly;
    private static readonly Assembly InfrastructureAssembly = typeof(FoodHub.Infrastructure.DependencyInjection).Assembly;

    private static bool References(Assembly assembly, string name) =>
        assembly.GetReferencedAssemblies().Any(a => a.Name == name);

    [Fact]
    public void Domain_Assembly_DoesNotReferenceOuterLayers()
    {
        Assert.False(References(DomainAssembly, "FoodHub.Application"));
        Assert.False(References(DomainAssembly, "FoodHub.Infrastructure"));
        Assert.False(References(DomainAssembly, "FoodHub.API"));
    }

    [Fact]
    public void Application_Assembly_DoesNotReferenceOuterLayers()
    {
        Assert.False(References(ApplicationAssembly, "FoodHub.Infrastructure"));
        Assert.False(References(ApplicationAssembly, "FoodHub.API"));
    }

    [Fact]
    public void Infrastructure_Assembly_DoesNotReferenceApi()
    {
        Assert.False(References(InfrastructureAssembly, "FoodHub.API"));
    }
}


