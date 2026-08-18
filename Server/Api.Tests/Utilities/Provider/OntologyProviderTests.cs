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
using InstanceService.Api.Utilities.Provider;
using InstanceService.Data;
using InstanceService.Models.Ontology;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace InstanceService.Api.Tests.Utilities.Provider;

/// <summary>
/// The provider must see every stored ontology, not just the most recently loaded one. Uploading a
/// second ontology used to hide the first: graph relation labels stopped resolving, and a hierarchy
/// uploaded as its own file never applied to the relations of another.
/// </summary>
public class OntologyProviderTests
{
    private static readonly Guid RelationsVersion = Guid.Parse("a7b603f6-1355-4f84-b73b-a5296e454736");
    private static readonly Guid HierarchyVersion = Guid.Parse("9d66f892-f433-47e8-b2c3-8e71782655b4");
    private static readonly Guid NewestVersion = Guid.Parse("e711eca0-02c7-4cd5-aaca-13f1de93b6a5");

    private static OntologyDbProvider CreateProvider(string databaseName, out InstanceServiceDbContext db)
    {
        var services = new ServiceCollection();
        services.AddDbContext<InstanceServiceDbContext>(o => o.UseInMemoryDatabase(databaseName));
        var provider = services.BuildServiceProvider();

        db = provider.CreateScope().ServiceProvider.GetRequiredService<InstanceServiceDbContext>();
        return new OntologyDbProvider(provider, Substitute.For<ILogger<OntologyDbProvider>>());
    }

    /// <summary>Three ontologies loaded at different times, the newest one being the smallest.</summary>
    private static void SeedThreeOntologies(InstanceServiceDbContext db)
    {
        db.OntologyVersions.AddRange(
            new OntologyVersion { Id = RelationsVersion, Etag = "big", LoadedAt = DateTimeOffset.UtcNow.AddMinutes(-4) },
            new OntologyVersion { Id = HierarchyVersion, Etag = "hierarchy", LoadedAt = DateTimeOffset.UtcNow.AddMinutes(-2) },
            new OntologyVersion { Id = NewestVersion, Etag = "newest", LoadedAt = DateTimeOffset.UtcNow });

        db.OntologyRelations.AddRange(
            new OntologyRelation
            {
                OntologyVersionId = RelationsVersion,
                PropertyUri = "https://gaeco.ekkodale.com/ontology/aggregates",
                Label = "aggregates",
                DomainUri = "ifc:IfcBuilding",
                RangeUri = "ifc:IfcSpace"
            },
            new OntologyRelation
            {
                OntologyVersionId = NewestVersion,
                PropertyUri = "http://gaeco.example.org/links#BuildingHasIfcBuilding",
                Label = "building has ifc building",
                DomainUri = "ex:Building",
                RangeUri = "ex:IfcBuilding"
            });

        // The class hierarchy arrived as its own upload, i.e. under a different version than the relations.
        db.OntologyClassHierarchies.Add(new OntologyClassHierarchy
        {
            OntologyVersionId = HierarchyVersion,
            ChildUri = "ifc:IfcSpaceBERTH",
            ParentUri = "ifc:IfcSpace"
        });

        db.SaveChanges();
    }

    [Fact]
    public async Task GetRelationLabelsAsync_ReturnsLabelsFromEveryStoredOntology()
    {
        var sut = CreateProvider(nameof(GetRelationLabelsAsync_ReturnsLabelsFromEveryStoredOntology), out var db);
        SeedThreeOntologies(db);

        var labels = await sut.GetRelationLabelsAsync();

        labels.Should().HaveCount(2);
        labels["https://gaeco.ekkodale.com/ontology/aggregates"].Should().Be("aggregates");
        labels["http://gaeco.example.org/links#BuildingHasIfcBuilding"].Should().Be("building has ifc building");
    }

    [Fact]
    public async Task GetAllRelationsAsync_IncludesRelationsOfOlderOntologies()
    {
        var sut = CreateProvider(nameof(GetAllRelationsAsync_IncludesRelationsOfOlderOntologies), out var db);
        SeedThreeOntologies(db);

        var relations = (await sut.GetAllRelationsAsync()).ToList();

        relations.Should().Contain(r => r.PropertyUri == "https://gaeco.ekkodale.com/ontology/aggregates");
        relations.Should().Contain(r => r.PropertyUri == "http://gaeco.example.org/links#BuildingHasIfcBuilding");
    }

    [Fact]
    public async Task GetAllRelationsAsync_AppliesHierarchyUploadedAsSeparateOntology()
    {
        var sut = CreateProvider(nameof(GetAllRelationsAsync_AppliesHierarchyUploadedAsSeparateOntology), out var db);
        SeedThreeOntologies(db);

        var relations = (await sut.GetAllRelationsAsync()).ToList();

        // IfcSpaceBERTH is a subclass of IfcSpace, so the aggregates relation must expand onto it
        // even though the hierarchy edge belongs to a different ontology version than the relation.
        relations.Should().Contain(r =>
            r.PropertyUri == "https://gaeco.ekkodale.com/ontology/aggregates"
            && r.DomainUri == "ifc:IfcBuilding"
            && r.RangeUri == "ifc:IfcSpaceBERTH");
    }

    [Fact]
    public async Task GetRelationLabelsAsync_WithoutAnyOntology_ReturnsEmpty()
    {
        var sut = CreateProvider(nameof(GetRelationLabelsAsync_WithoutAnyOntology_ReturnsEmpty), out _);

        var labels = await sut.GetRelationLabelsAsync();

        labels.Should().BeEmpty();
    }
}
