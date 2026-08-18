// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Data.Options;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using System.Text.Json.Serialization;


/// <summary>
/// Interface for the UserGroupProvider
/// </summary>
public interface IUserGroupProvider
{
    Task<List<string>> GetUserGroupIdsAsync(string token);

    Task<string> GetAccessTokenAsync();
}

/// <summary>
/// Utility class to provide user groups by fetching them from keycloak.
/// </summary>
public class UserGroupProvider : IUserGroupProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _keycloakBaseUrl;
    private readonly string _realm;
    private readonly string _clientId;
    private readonly string _clientSecret;

    public UserGroupProvider(HttpClient httpClient, IOptions<KeycloakOptions> keyloakOptions)
    {
        var keycloak = keyloakOptions.Value;
        _httpClient = httpClient;
        _keycloakBaseUrl = keycloak.ServerUrl;
        _realm = keycloak.Realm;
        _clientId = keycloak.ClientId;
        _clientSecret = keycloak.ClientSecret;
    }

    /// <summary>
    /// Retrieves user groups based on the token.
    /// </summary>
    /// <param name="token">The bearer token containing the group information.</param>
    /// <returns></returns>
    public async Task<List<string>> GetUserGroupIdsAsync(string token)
    {
        var groupIds = new List<string>();

        // Extract the JWT token
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Extract group names from the JWT token
        var groupClaims = jwtToken.Claims
            .Where(c => c.Type == "groups") // TODO: This is hardcoded, claim should be defined in Program.cs
            .Select(c => c.Value)
            .ToList();

        if (groupClaims != null)
        {
            var accessToken = await GetAccessTokenAsync();

            // Fetch all groups from Keycloak
            var allGroups = await ListAllGroupsAsync(accessToken);

            foreach (var groupName in groupClaims)
            {
                // Match the group name with the fetched groups
                var matchedGroup = allGroups.FirstOrDefault(g => groupName.EndsWith($"/{g.Name}", StringComparison.OrdinalIgnoreCase) || groupName.Equals(g.Name, StringComparison.OrdinalIgnoreCase));
                if (matchedGroup != null)
                {
                    groupIds.Add(matchedGroup.Id);
                }
            }
        }

        return groupIds;
    }

    /// <summary>
    /// Authenticate with Keycloak using client credentials and get an access token
    /// </summary>
    public async Task<string> GetAccessTokenAsync()
    {
        var tokenUrl = $"{_keycloakBaseUrl}/realms/{_realm}/protocol/openid-connect/token";
        var requestContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("client_id", _clientId),
            new KeyValuePair<string, string>("client_secret", _clientSecret),
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        var response = await _httpClient.PostAsync(tokenUrl, requestContent);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<KeycloakTokenResponse>(jsonResponse);
            return tokenResponse.AccessToken;
        }

        throw new Exception("Failed to retrieve access token from Keycloak");
    }

    /// <summary>
    /// Fetch group details from Keycloak using an access token
    /// </summary>
    /// <param name="accessToken">The retrieved access token.</param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private async Task<List<KeycloakGroup>> ListAllGroupsAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_keycloakBaseUrl}/admin/realms/{_realm}/groups");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            var jsonResponse = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            return JsonSerializer.Deserialize<List<KeycloakGroup>>(jsonResponse, options);
        }

        throw new Exception("Failed to retrieve groups from Keycloak");
    }

    private class KeycloakGroup
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    private class KeycloakTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;
    }
}