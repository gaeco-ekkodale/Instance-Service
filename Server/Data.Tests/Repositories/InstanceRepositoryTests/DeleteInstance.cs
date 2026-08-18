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
using InstanceService.Models;

namespace InstanceService.Data.Tests.Repositories.InstanceRepositoryTests;

[Collection(nameof(DatabaseTestCollection))]
[Trait("Category", "Integration")]
public class DeleteInstance
{
    private readonly GraphTraversalSource g;
    private readonly InstanceServiceDbContext dbContext;

    private readonly GremlinInstanceRepository instanceRepository;

    public DeleteInstance(ArcadeDbDatabaseFixture graphFixture, DbContextFixture dbContextFixture)
    {
        g = graphFixture.TraversalSource;
        dbContext = dbContextFixture.DbContext;

        instanceRepository = new GremlinInstanceRepository(g, dbContext);
    }

    [Fact]
    public async Task When_IdNotExists_Then_NotFoundException()
    {
        // Act
        Func<Task> act = async () => await instanceRepository.DeleteInstance("notExistingId");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task When_IdExists_Then_Removed()
    {
        // Arrange
        var id = await instanceRepository.CreateInstance("name", "classificaiton", []);

        // Act
        await instanceRepository.DeleteInstance(id);

        // Assert
        var vertexCount = await g.V().Has("Instance", "Id", id).Count().Promise(t => t.Next());
        vertexCount.Should().Be(0);

        dbContext.InstanceMetadata.SingleOrDefault(i => i.Id == id).Should().BeNull();
    }

    [Fact]
    public async Task When_NodeHasRelations_Then_RelationsAreRemoved()
    {
        // Arrange
        var id = await instanceRepository.CreateInstance("name", "classification", []);
        var id2 = await instanceRepository.CreateInstance("name2", "classification2", []);
        await instanceRepository.CreateRelations(new InstanceRelation { SubjectId = id, ObjectId = id2, PredicateUri = "RELATES_TO" });

        // Act
        await instanceRepository.DeleteInstance(id);

        // Assert
        var vertexCount = await g.V().Has("Instance", "Id", id).Count().Promise(t => t.Next());
        vertexCount.Should().Be(0);

        var edgeCount = await g.E().HasLabel("RELATES_TO").Count().Promise(t => t.Next());
        edgeCount.Should().Be(0);
    }
}
