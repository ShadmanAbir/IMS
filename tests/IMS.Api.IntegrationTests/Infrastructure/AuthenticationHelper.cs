using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Text.Json;
using IMS.Api.Domain.Entities;

namespace IMS.Api.IntegrationTests.Infrastructure;

public static class AuthenticationHelper
{
    public static async Task<string> GetJwtTokenAsync(HttpClient client, string email = "testuser@example.com", string password = "TestPass123!")
    {
        var loginRequest = new
        {
            Email = email,
            Password = password
        };

        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            System.Text.Encoding.UTF8,
            "application/json");

        var loginResponse = await client.PostAsync("/api/v1/auth/login", loginContent);
        
        if (!loginResponse.IsSuccessStatusCode)
        {
            var errorContent = await loginResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to authenticate: {loginResponse.StatusCode} - {errorContent}");
        }

        var loginResult = await loginResponse.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(loginResult, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return tokenResponse?.AccessToken ?? throw new InvalidOperationException("No access token received");
    }

    public static void SetAuthorizationHeader(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<HttpClient> CreateAuthenticatedClientAsync(IntegrationTestWebApplicationFactory<Program> factory, string email = "testuser@example.com")
    {
        var client = factory.CreateClient();
        var token = await GetJwtTokenAsync(client, email);
        SetAuthorizationHeader(client, token);
        return client;
    }

    private class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = string.Empty;
    }
}