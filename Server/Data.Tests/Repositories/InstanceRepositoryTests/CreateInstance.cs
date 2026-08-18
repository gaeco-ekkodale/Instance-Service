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
using InstanceService.Models;
using Gremlin.Net.Process.Traversal;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace InstanceService.Data.Tests.Repositories.InstanceRepositoryTests;

[Collection(nameof(DatabaseTestCollection))]
[Trait("Category", "Integration")]
public class CreateInstance
{
    private readonly GraphTraversalSource g;
    private readonly InstanceServiceDbContext dbContext;
    private readonly DbContextOptions<InstanceServiceDbContext> dbContextOptions;

    private readonly GremlinInstanceRepository instanceRepository;

    public CreateInstance(ArcadeDbDatabaseFixture graphFixture, DbContextFixture dbContextFixture)
    {
        g = graphFixture.TraversalSource;

        dbContextOptions = dbContextFixture.DbContextOptions;
        dbContext = dbContextFixture.DbContext;

        instanceRepository = new GremlinInstanceRepository(g, dbContext);
    }

    [Fact]
    public async Task When_CorrectInput_Then_BothDatabaseCorrectlyFilled()
    {
        // Arrange
        var name = "TestInstance";
        var classificationId = "TestClassification";
        var data = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
        };

        // Act
        var id = await instanceRepository.CreateInstance(name, classificationId, data);

        // Assert
        id.Should().NotBeNull();

        var vertices = await g.V().Has("Instance", "Id", id)
            .ElementMap<object>()
            .Promise(t => t.ToList());

        var vertex = vertices.Should().ContainSingle().Subject;
        vertex["Name"]?.ToString().Should().Be(name);
        vertex["ClassificationId"]?.ToString().Should().Be(classificationId);

        var instance = dbContext.InstanceMetadata.FirstOrDefault(i => i.Id == id);
        instance.Should().NotBeNull();
        instance!.Name.Should().Be(name);
        instance!.ClassificationId.Should().Be(classificationId);
        instance!.Properties.Should().BeEquivalentTo(data);
    }

    [Fact]
    public async Task When_PostgresHasError_Then_DatabaseException()
    {
        // Arrange
        var name = "TestInstance";
        var classificationId = "TestClassification";
        var data = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" },
        };

        var dbContext = Substitute.For<InstanceServiceDbContext>(dbContextOptions);
        dbContext.Add(Arg.Any<InstanceMetaData>()).Returns(x => throw new Exception());

        var instanceRepository = new GremlinInstanceRepository(g, dbContext);

        // Act
        Func<Task> act = async () => await instanceRepository.CreateInstance(name, classificationId, data);

        // Assert
        await act.Should().ThrowAsync<DatabaseException>();
    }
}
