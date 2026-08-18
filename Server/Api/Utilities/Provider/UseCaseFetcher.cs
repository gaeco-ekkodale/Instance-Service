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

namespace InstanceService.Api.Utilities.Provider
{
    public class UseCaseFetcher : IUseCaseFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<UseCaseFetcher> _logger;
        private readonly string _useCaseUrl;
        private readonly IUserGroupProvider _tokenProvider;

        public UseCaseFetcher (
            HttpClient httpClient,
            IOptions<UsecaseOptions> usecaseOptions,
            ILogger<UseCaseFetcher> logger,
            IUserGroupProvider tokenProvider)
        {
            _httpClient = httpClient;
            _logger = logger;
            _useCaseUrl = usecaseOptions.Value.Address;
            _tokenProvider = tokenProvider;
        }

        ///<inheritdoc/>
        public async Task<UseCase> GetUseCasesByIdAsync(string useCaseId)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", await _tokenProvider.GetAccessTokenAsync());
                var response = await _httpClient.GetAsync($"{_useCaseUrl}/api/UseCases/{useCaseId}");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonConvert.DeserializeObject<UseCase>(content);
                if (data == null)
                {
                    _logger.LogWarning("Usecase {Usecase} is null", useCaseId);
                    throw new Exception("Data of usecase is null");
                }
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching usecases.");
                throw;
            }
        }
    }
}
