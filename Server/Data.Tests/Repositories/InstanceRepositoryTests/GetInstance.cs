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
public class GetInstance
{
    private readonly GraphTraversalSource g;
    private readonly InstanceServiceDbContext dbContext;

    private readonly GremlinInstanceRepository instanceRepository;

    public GetInstance(ArcadeDbDatabaseFixture graphFixture, DbContextFixture dbContextFixture)
    {
        g = graphFixture.TraversalSource;
        dbContext = dbContextFixture.DbContext;

        instanceRepository = new GremlinInstanceRepository(g, dbContext);
    }

    [Fact]
    public async Task When_NodeNotExists_Then_Null()
    {
        // Act
        var instance = await instanceRepository.GetInstance("notExistingId");

        // Assert
        instance.Should().BeNull();
    }

    [Fact]
    public async Task When_NodeExists_Then_InstanceReturned()
    {
        // Arrange
        var id = await instanceRepository.CreateInstance("name", "classification", new Dictionary<string, string> { { "key1", "value1" } });
        await instanceRepository.CreateInstance("name2", "classification2", []);
        await instanceRepository.CreateInstance("name3", "classification3", []);
        await instanceRepository.CreateInstance("name4", "classification4", []);

        // Act
        var instance = await instanceRepository.GetInstance(id);

        // Assert
        instance.Should().NotBeNull();
        instance!.Id.Should().Be(id);
        instance.Name.Should().Be("name");
        instance.ClassificationId.Should().Be("classification");
        instance.Properties.Should().ContainSingle();
    }

    [Fact]
    public async Task When_NodeWithRelations_Then_InstanceFilledWithReturned()
    {
        // Arrange
        var id = await instanceRepository.CreateInstance("name", "classification", new Dictionary<string, string> { { "key1", "value1" } });
        var id2 = await instanceRepository.CreateInstance("name2", "classification2", []);
        var id3 = await instanceRepository.CreateInstance("name3", "classification3", []);
        await instanceRepository.CreateRelations(
            new Models.InstanceRelation { SubjectId = id, ObjectId = id2, PredicateUri = "RELATES_TO" },
            new Models.InstanceRelation { SubjectId = id, ObjectId = id2, PredicateUri = "OTHER_LABEL" },
            new Models.InstanceRelation { SubjectId = id, ObjectId = id3, PredicateUri = "RELATES_TO" },
            new Models.InstanceRelation { SubjectId = id2, ObjectId = id, PredicateUri = "RELATES_TO" }
            );

        // Act
        var instance = await instanceRepository.GetInstance(id);

        // Assert
        instance.Should().NotBeNull();
        instance!.Id.Should().Be(id);
        instance.Name.Should().Be("name");
        instance.ClassificationId.Should().Be("classification");
        instance.Properties.Should().ContainSingle();

        instance.Relations.Where(r => r.SubjectId == id).Should().HaveCount(3);
        instance.Relations.Where(r => r.ObjectId == id).Should().HaveCount(1);

        // PredicateUri must round-trip through persistence: the two id→id2 edges are distinguished
        // only by their predicate, so both distinct URIs have to survive the graph round-trip.
        instance.Relations.Where(r => r.SubjectId == id && r.ObjectId == id2)
            .Select(r => r.PredicateUri)
            .Should().BeEquivalentTo("RELATES_TO", "OTHER_LABEL");
        instance.Relations.Should().Contain(r => r.SubjectId == id && r.ObjectId == id3 && r.PredicateUri == "RELATES_TO");
    }

    [Fact]
    public async Task When_GraphIsInconsistent_Then_DatabaseException()
    {
        // Arrange: metadata exists in PostgreSQL but no vertex in graph
        var id = "graphInconsistent_" + Guid.NewGuid();
        dbContext.InstanceMetadata.Add(new Models.InstanceMetaData
        {
            Id = id,
            Name = "name",
            ClassificationId = "classification",
            Properties = new Dictionary<string, string> { { "key1", "value1" } }
        });
        await dbContext.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await instanceRepository.GetInstance(id);

        // Assert
        await act.Should().ThrowAsync<DatabaseException>();
    }

    [Fact]
    public async Task When_RelationalDBIsInconsistent_Then_DatabaseException()
    {
        // Arrange: vertex exists in graph but no metadata in PostgreSQL
        var id = "pgInconsistent_" + Guid.NewGuid();
        await g.AddV("Instance")
            .Property("Id", id)
            .Property("Name", "name")
            .Property("ClassificationId", "classification")
            .Promise(t => t.Iterate());

        // Act
        Func<Task> act = async () => await instanceRepository.GetInstance(id);

        // Assert
        await act.Should().ThrowAsync<DatabaseException>();
    }
}
