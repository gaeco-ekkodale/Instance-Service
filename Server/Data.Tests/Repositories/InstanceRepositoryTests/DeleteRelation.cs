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
public class DeleteRelation
{
    private readonly GraphTraversalSource g;
    private readonly InstanceServiceDbContext dbContext;

    private readonly GremlinInstanceRepository instanceRepository;

    public DeleteRelation(ArcadeDbDatabaseFixture graphFixture, DbContextFixture dbContextFixture)
    {
        g = graphFixture.TraversalSource;
        dbContext = dbContextFixture.DbContext;

        instanceRepository = new GremlinInstanceRepository(g, dbContext);
    }

    [Fact]
    public async Task When_MultiplePredicatesBetweenSamePair_Then_OnlyMatchingEdgeIsDeleted()
    {
        // Arrange: two distinct relations between the same pair, distinguished only by predicate URI
        var id = await instanceRepository.CreateInstance("name", "classification", []);
        var id2 = await instanceRepository.CreateInstance("name2", "classification2", []);
        await instanceRepository.CreateRelations(
            new InstanceRelation { SubjectId = id, ObjectId = id2, PredicateUri = "RELATES_TO" },
            new InstanceRelation { SubjectId = id, ObjectId = id2, PredicateUri = "OTHER_LABEL" });

        // Act: delete only the RELATES_TO relation
        await instanceRepository.DeleteRelation(id, id2, "RELATES_TO");

        // Assert: the other predicate between the same pair must survive
        var instance = await instanceRepository.GetInstance(id);
        instance!.Relations.Where(r => r.SubjectId == id && r.ObjectId == id2)
            .Select(r => r.PredicateUri)
            .Should().ContainSingle().Which.Should().Be("OTHER_LABEL");
    }
}
