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
using InstanceService.Models;
using Gremlin.Net.Process.Traversal;

namespace InstanceService.Data.Tests.Repositories.InstanceRepositoryTests;

[Collection(nameof(DatabaseTestCollection))]
[Trait("Category", "Integration")]
public class CreateRelations
{
    private readonly GraphTraversalSource g;
    private readonly InstanceServiceDbContext dbContext;

    private readonly GremlinInstanceRepository instanceRepository;

    public CreateRelations(ArcadeDbDatabaseFixture graphFixture, DbContextFixture dbContextFixture)
    {
        g = graphFixture.TraversalSource;
        dbContext = dbContextFixture.DbContext;

        instanceRepository = new GremlinInstanceRepository(g, dbContext);
    }

    [Fact]
    public async Task When_SingleRelation_Then_Success()
    {
        // Arrange
        await g.AddV("Instance").Property("Id", "subjectId").Promise(t => t.Iterate());
        await g.AddV("Instance").Property("Id", "objectId").Promise(t => t.Iterate());

        var relation = new InstanceRelation
        {
            SubjectId = "subjectId",
            ObjectId = "objectId",
            PredicateUri = "RELATES_TO",
        };

        // Act
        await instanceRepository.CreateRelations(relation);

        // Assert
        var edgeCount = await g.V().Has("Instance", "Id", "subjectId")
            .OutE("RELATES_TO")
            .Where(__.InV().Has("Instance", "Id", "objectId"))
            .Count()
            .Promise(t => t.Next());

        edgeCount.Should().Be(1);
    }

    [Fact]
    public async Task When_MultipleRelations_Then_Success()
    {
        // Arrange
        await g.AddV("Instance").Property("Id", "subjectId1").Promise(t => t.Iterate());
        await g.AddV("Instance").Property("Id", "objectId1").Promise(t => t.Iterate());
        await g.AddV("Instance").Property("Id", "subjectId2").Promise(t => t.Iterate());
        await g.AddV("Instance").Property("Id", "objectId2").Promise(t => t.Iterate());

        var relations = new[]
        {
            new InstanceRelation
            {
                SubjectId = "subjectId1",
                ObjectId = "objectId1",
                PredicateUri = "RELATES_TO",
            },
            new InstanceRelation
            {
                SubjectId = "subjectId2",
                ObjectId = "objectId2",
                PredicateUri = "RELATES_TO",
            },
            new InstanceRelation
            {
                SubjectId = "subjectId1",
                ObjectId = "objectId2",
                PredicateUri = "RELATES_TO",
            },
        };

        // Act
        await instanceRepository.CreateRelations(relations);

        // Assert
        var edgeCount = await g.E().HasLabel("RELATES_TO").Count().Promise(t => t.Next());
        edgeCount.Should().Be(3);
    }

    [Fact]
    public async Task When_RelationIsExisting_Then_NothingHappens()
    {
        // Arrange
        var id = await instanceRepository.CreateInstance("name", "classification", []);
        var id2 = await instanceRepository.CreateInstance("name2", "classification", []);
        await instanceRepository.CreateRelations(new InstanceRelation { SubjectId = id, ObjectId = id2, PredicateUri = "RELATES_TO" });

        // Act
        await instanceRepository.CreateRelations(new InstanceRelation { SubjectId = id, ObjectId = id2, PredicateUri = "RELATES_TO" });

        // Assert
        var edgeCount = await g.E().HasLabel("RELATES_TO").Count().Promise(t => t.Next());
        edgeCount.Should().Be(1);
    }
}
