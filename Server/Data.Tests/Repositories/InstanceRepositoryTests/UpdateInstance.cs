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
using InstanceService.Data.Exceptions;
using InstanceService.Data.Repositories;
using InstanceService.Data.Tests.TestUtils.Collection;
using InstanceService.Data.Tests.TestUtils.Fixtures;
using Gremlin.Net.Process.Traversal;

namespace InstanceService.Data.Tests.Repositories.InstanceRepositoryTests;

[Collection(nameof(DatabaseTestCollection))]
[Trait("Category", "Integration")]
public class UpdateInstance
{
    private readonly GraphTraversalSource g;
    private readonly InstanceServiceDbContext dbContext;

    private readonly GremlinInstanceRepository instanceRepository;

    public UpdateInstance(ArcadeDbDatabaseFixture graphFixture, DbContextFixture dbContextFixture)
    {
        g = graphFixture.TraversalSource;
        dbContext = dbContextFixture.DbContext;

        instanceRepository = new GremlinInstanceRepository(g, dbContext);
    }

    [Fact]
    public async Task When_NodeNotExists_Then_NotFoundException()
    {
        // Act
        Func<Task> act = async () => await instanceRepository.UpdateInstance("notExistingId", "name", []);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task When_NodeExists_Then_BothDatabaseAreUpdated()
    {
        // Arrange
        var id = await instanceRepository.CreateInstance("name", "classification", []);

        // Act
        await instanceRepository.UpdateInstance(id, "newName", new Dictionary<string, string> { { "key1", "value1" } });

        // Assert
        var vertices = await g.V().Has("Instance", "Id", id)
            .ElementMap<object>()
            .Promise(t => t.ToList());

        var vertex = vertices.Should().ContainSingle().Subject;
        vertex["Name"]?.ToString().Should().Be("newName");

        var instance = dbContext.InstanceMetadata.FirstOrDefault(i => i.Id == id);
        instance.Should().NotBeNull();
        instance!.Name.Should().Be("newName");
        instance.Properties.Should().ContainSingle();
    }

    [Fact]
    public async Task When_PostgresMetadataMissing_Then_NotFoundException()
    {
        // Arrange: vertex exists in graph but no metadata in PostgreSQL
        var id = "existingId";
        await g.AddV("Instance")
            .Property("Id", id)
            .Property("Name", "name")
            .Promise(t => t.Iterate());

        // Act
        Func<Task> act = async () => await instanceRepository.UpdateInstance(id, "newName", new Dictionary<string, string> { { "key1", "value1" } });

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
