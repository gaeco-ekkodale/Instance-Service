// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using FluentAssertions;
using InstanceService.Data.Repositories;
using InstanceService.Data.Tests.TestUtils.Collection;
using InstanceService.Data.Tests.TestUtils.Fixtures;
using Gremlin.Net.Process.Traversal;

namespace InstanceService.Data.Tests.Repositories.InstanceRepositoryTests;

[Collection(nameof(DatabaseTestCollection))]
[Trait("Category", "Integration")]
public class GetInstances
{
    private readonly GraphTraversalSource g;
    private readonly InstanceServiceDbContext dbContext;

    private readonly GremlinInstanceRepository instanceRepository;

    public GetInstances(ArcadeDbDatabaseFixture graphFixture, DbContextFixture dbContextFixture)
    {
        g = graphFixture.TraversalSource;
        dbContext = dbContextFixture.DbContext;

        instanceRepository = new GremlinInstanceRepository(g, dbContext);
    }

    [Fact]
    public async Task When_NoInstances_Then_ReturnEmpty()
    {
        // Act
        var instances = await instanceRepository.GetInstances();

        // Assert
        instances.Should().BeEmpty();
    }

    [Fact]
    public async Task When_WithOutMetadata_Then_SimpleInformationRetrieved()
    {
        // Arrange
        await instanceRepository.CreateInstance("name", "classification", new Dictionary<string, string> { { "key1", "value1" } });
        await instanceRepository.CreateInstance("name2", "classification2", new Dictionary<string, string> { { "key1", "value1" } });
        await instanceRepository.CreateInstance("name2", "classification2", new Dictionary<string, string> { { "key1", "value1" } });

        // Act
        var instances = await instanceRepository.GetInstances();

        // Assert
        instances.Should().HaveCount(3);
        instances.Should().OnlyContain(instance => instance.Properties.Count == 0);
    }
}
