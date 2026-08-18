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
public class DeleteRelations
{
    private readonly GraphTraversalSource g;
    private readonly InstanceServiceDbContext dbContext;

    private readonly GremlinInstanceRepository instanceRepository;

    public DeleteRelations(ArcadeDbDatabaseFixture graphFixture, DbContextFixture dbContextFixture)
    {
        g = graphFixture.TraversalSource;
        dbContext = dbContextFixture.DbContext;

        instanceRepository = new GremlinInstanceRepository(g, dbContext);
    }

    [Fact]
    public async Task When_NodeNotExists_Then_NotFoundException()
    {
        // Act
        Func<Task> act = async () => await instanceRepository.DeleteRelations("notExistingId");

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task When_NotExistsButNoRelation_Then_Success()
    {
        // Arrange
        var id = "existingId";
        await g.AddV("Instance").Property("Id", id).Promise(t => t.Iterate());
        await g.AddV("Instance").Property("Id", "otherId").Promise(t => t.Iterate());

        // Act
        await instanceRepository.DeleteRelations(id);

        // Assert
        var vertexCount = await g.V().Has("Instance", "Id", id).Count().Promise(t => t.Next());
        vertexCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task When_RelationsExists_Then_Success()
    {
        // Arrange
        var id = "existingId";
        await g.AddV("Instance").Property("Id", id).Promise(t => t.Iterate());
        await g.AddV("Instance").Property("Id", "otherId").Promise(t => t.Iterate());
        await g.AddV("Instance").Property("Id", "anotherId").Promise(t => t.Iterate());

        // n -> m
        await g.V().Has("Instance", "Id", id).As("s")
            .V().Has("Instance", "Id", "otherId")
            .AddE("RELATES_TO").From("s")
            .Promise(t => t.Iterate());

        // m -> n
        await g.V().Has("Instance", "Id", "otherId").As("s")
            .V().Has("Instance", "Id", id)
            .AddE("RELATES_TO").From("s")
            .Promise(t => t.Iterate());

        // m -> o
        await g.V().Has("Instance", "Id", "otherId").As("s")
            .V().Has("Instance", "Id", "anotherId")
            .AddE("RELATES_TO").From("s")
            .Promise(t => t.Iterate());

        // Act
        await instanceRepository.DeleteRelations(id);

        // Assert
        var vertexCount = await g.V().Has("Instance", "Id", id).Count().Promise(t => t.Next());
        vertexCount.Should().BeGreaterThan(0);

        var edgeCount = await g.E().HasLabel("RELATES_TO").Count().Promise(t => t.Next());
        edgeCount.Should().Be(1);
    }
}
