using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SceneSplit.Api.Extenstions;
using SceneSplit.Configuration;

namespace SceneSplit.Api.UnitTests.Extenstions;

[TestFixture]
internal class ServiceCollectionExtensionsTests
{
    private IServiceCollection services;
    private Mock<IConfiguration> configurationMock;

    [SetUp]
    public void SetUp()
    {
        configurationMock = new Mock<IConfiguration>();

        services = new ServiceCollection();
    }

    [TestCase("http://example.com,http://test.com", true, 2)]
    [TestCase("", true, 0)]
    [TestCase("http://localhost,http://prod.com", false, 2)]
    public void AddApplicationCors_RegistersCorsWithCorrectOrigins(string allowedOrigins, bool isDevelopment, int expectedCount)
    {
        // Arrange
        configurationMock.Setup(c => c[ApiConfigurationKeys.ALLOWED_CORS_ORIGINS]).Returns(allowedOrigins);

        services.AddLogging();
        services.AddRouting();

        // Act
        services.AddApplicationCors(configurationMock.Object, "TestPolicy", isDevelopment);

        var serviceProvider = services.BuildServiceProvider();
        var corsService = serviceProvider.GetRequiredService<ICorsService>();

        // Assert
        Assert.That(corsService, Is.Not.Null);
        Assert.That(services.Any(s => s.ServiceType == typeof(ICorsService)));

        var policyProvider = serviceProvider.GetRequiredService<ICorsPolicyProvider>();
        var policy = policyProvider.GetPolicyAsync(new DefaultHttpContext(), "TestPolicy").Result;

        Assert.That(policy?.Origins.Count, Is.EqualTo(expectedCount));
    }

    [Test]
    public void AddApplicationCors_RegistersCorsWithAnyOriginAndHeaderAndMethod()
    {
        // Arrange
        configurationMock.Setup(c => c[ApiConfigurationKeys.ALLOWED_CORS_ORIGINS]).Returns("*");

        services.AddLogging();
        services.AddRouting();

        // Act
        services.AddApplicationCors(configurationMock.Object, "TestPolicy", isDevelopment: false);

        var serviceProvider = services.BuildServiceProvider();
        var corsService = serviceProvider.GetRequiredService<ICorsService>();

        // Assert
        Assert.That(corsService, Is.Not.Null);
        Assert.That(services.Any(s => s.ServiceType == typeof(ICorsService)));

        var policyProvider = serviceProvider.GetRequiredService<ICorsPolicyProvider>();
        var policy = policyProvider.GetPolicyAsync(new DefaultHttpContext(), "TestPolicy").Result;

        Assert.That(policy?.AllowAnyOrigin, Is.True);
        Assert.That(policy?.AllowAnyHeader, Is.True);
        Assert.That(policy?.AllowAnyMethod, Is.True);
    }

    [Test]
    public void AddApplicationCors_DevelopmentMode_AllowsLocalhost()
    {
        // Arrange
        configurationMock.Setup(c => c[ApiConfigurationKeys.ALLOWED_CORS_ORIGINS]).Returns("*");

        services.AddLogging();
        services.AddRouting();

        // Act
        services.AddApplicationCors(configurationMock.Object, "DevPolicy", true);

        var serviceProvider = services.BuildServiceProvider();
        var policyProvider = serviceProvider.GetRequiredService<ICorsPolicyProvider>();
        var policy = policyProvider.GetPolicyAsync(new DefaultHttpContext(), "DevPolicy").Result;

        // Assert
        Assert.That(policy, Is.Not.Null);
        Assert.That(policy.IsOriginAllowed("http://localhost"), Is.True);
    }
}