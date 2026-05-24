using Clients.Application.DTOs;
using Clients.Domain.Entities;
using Clients.Infrastructure.Persistence;
using Clients.Tests.Integration.Factory;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Clients.Tests.Integration;

public class ClientsApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ClientsApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetClient_WithValidToken_ShouldReturnClientDetails()
    {
        // Arrange
        var clientId = Guid.NewGuid();
        var newClient = Client.Create("Integration Test User", "integration@test.com", "Rua X", "0001", "12345-6");

        typeof(Client).GetProperty("Id")!.SetValue(newClient, clientId);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ClientsDbContext>();
            db.Clients.Add(newClient);
            await db.SaveChangesAsync();
        }

        // Act
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { Username = "admin", Password = "Admin@123" });
        loginResponse.EnsureSuccessStatusCode();

        var loginContent = await loginResponse.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<JsonElement>(loginContent).GetProperty("token").GetString();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.GetAsync($"/api/v1/clients/{clientId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var returnedClient = await response.Content.ReadFromJsonAsync<ClientResponseDto>();
        returnedClient.Should().NotBeNull();
        returnedClient!.Id.Should().Be(clientId);
        returnedClient.Name.Should().Be("Integration Test User");
        returnedClient.Email.Should().Be("integration@test.com");
    }

    [Fact]
    public async Task GetClient_WithoutToken_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/clients/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
