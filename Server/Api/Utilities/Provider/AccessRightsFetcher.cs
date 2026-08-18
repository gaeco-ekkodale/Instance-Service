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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using InstanceService.Models;
using System.Net.Http.Headers;

namespace InstanceService.Api.Utilities.Provider;

/// <summary>
/// Utility class to provide access rights by fetching them from the Access Service API
/// </summary>
public class AccessRightsFetcher : IAccessRightsFetcher
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly string _cacheKey = "ACCESSRIGHTS";
    private readonly ILogger<AccessRightsFetcher> _logger;
    private readonly string _accessRightsUrl;
    private readonly IUserGroupProvider _tokenProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessRightsFetcher"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client to make API requests.</param>
    /// <param name="cache">The memory cache for storing access rights.</param>
    /// <param name="accessOptions">The configuration options for the Access Service.</param>
    /// <param name="logger">The logger for recording information and errors.</param>
    /// <param name="tokenProvider">The provider for retrieving access tokens.</param>
    public AccessRightsFetcher(HttpClient httpClient, IMemoryCache cache, IOptions<AccessOptions> accessOptions, ILogger<AccessRightsFetcher> logger, IUserGroupProvider tokenProvider)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
        _accessRightsUrl = accessOptions.Value.Address;
        _tokenProvider = tokenProvider;
    }

    /// <summary>
    /// Asynchronously retrieves all access rights from the Access Service API.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a collection of <see cref="AccessRight"/>.
    /// </returns>
    /// <exception cref="HttpRequestException">Thrown if the HTTP request fails.</exception>
    public async Task<IEnumerable<AccessRight>> GetAccessRightsAsync()
    {
        // TODO Cache data is not refetched if it updates I think? Because it sees the data with that key is there... need to increment the key or sth?
        //if (_cache.TryGetValue(_cacheKey, out IEnumerable<AccessRight> cachedData))
        //{
        //    return cachedData;
        //}
         
        try
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", await _tokenProvider.GetAccessTokenAsync());
            var response = await _httpClient.GetAsync($"{_accessRightsUrl}/api/AccessRights");  //TODO will eventually be done via kafka topics
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<List<AccessRight>>(content);

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                Size = 1024,
            };
            _cache.Set(_cacheKey, data, cacheEntryOptions);

            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching access rights.");
            throw;
        }
    }

    /// <summary>
    /// Asynchronously retrieves access rights for a specific use case and user group.
    /// </summary>
    /// <param name="userGroupId">The identifier of the user group.</param>
    /// <param name="useCaseId">The identifier of the use case.</param>
    /// <returns>
    /// A task that represents the asynchronous operation.
    /// The task result contains a collection of <see cref="AccessRight"/> for the specified IDs, or <c>null</c> if the retrieval fails.
    /// </returns>
    /// <exception cref="HttpRequestException">Thrown if the HTTP request fails.</exception>
    public async Task<IEnumerable<AccessRight>?> GetAccessRightsByUseCaseUserGroupAsync(string userGroupId, string useCaseId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_accessRightsUrl}/api/AccessRights/usecase/{useCaseId}/usergroup/{userGroupId}");  //TODO will eventually be done via kafka topics
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var accessRights = JsonConvert.DeserializeObject<List<AccessRight>>(content);

            return accessRights;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching access rights for user group {UserGroup} and use case {UseCase}.",
                userGroupId, useCaseId);
            throw;
        }
    }
}