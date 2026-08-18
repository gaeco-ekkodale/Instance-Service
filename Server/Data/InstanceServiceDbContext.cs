// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Models;
using InstanceService.Models.Guideline;
using InstanceService.Models.Ontology;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace InstanceService.Data;

public class InstanceServiceDbContext : DbContext
{
    public InstanceServiceDbContext(DbContextOptions<InstanceServiceDbContext> options) : base(options)
    {
    }

    public DbSet<InstanceMetaData> InstanceMetadata { get; set; }

    public DbSet<OntologyVersion> OntologyVersions { get; set; }
    public DbSet<OntologyClassHierarchy> OntologyClassHierarchies { get; set; }
    public DbSet<OntologyRelation> OntologyRelations { get; set; }

    public DbSet<GuidelineVersion> GuidelineVersions { get; set; }
    public DbSet<GuidelineClassification> GuidelineClassifications { get; set; }
    public DbSet<GuidelinePropertySet> GuidelinePropertySets { get; set; }
    public DbSet<GuidelineProperty> GuidelineProperties { get; set; }
    public DbSet<GuidelineClassificationProperty> GuidelineClassificationProperties { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InstanceMetaData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Properties).HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<Dictionary<string, string>>(v) ?? new Dictionary<string, string>()
            ).HasColumnType("jsonb");
        });

        modelBuilder.Entity<OntologyVersion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // PK comes from the event, never auto-generated
        });

        modelBuilder.Entity<OntologyClassHierarchy>(entity =>
        {
            entity.HasKey(e => new { e.OntologyVersionId, e.ChildUri, e.ParentUri });
            entity.HasIndex(e => new { e.OntologyVersionId, e.ChildUri });
            entity.HasOne<OntologyVersion>()
                .WithMany()
                .HasForeignKey(e => e.OntologyVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OntologyRelation>(entity =>
        {
            entity.HasKey(e => new { e.OntologyVersionId, e.PropertyUri, e.DomainUri, e.RangeUri });
            entity.HasIndex(e => new { e.OntologyVersionId, e.DomainUri });
            entity.HasIndex(e => new { e.OntologyVersionId, e.RangeUri });
            entity.HasOne<OntologyVersion>()
                .WithMany()
                .HasForeignKey(e => e.OntologyVersionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Guideline relational projection ──────────────────────────────────
        modelBuilder.Entity<GuidelineVersion>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToTable("guideline_version");
            e.Property(x => x.Id).ValueGeneratedNever(); // PK comes from the event, never auto-generated
            e.HasIndex(x => new { x.ObjectName, x.Etag }).IsUnique();
            e.HasIndex(x => x.ProcessedAt).IsDescending();
            e.Property(x => x.MappingsJson).HasColumnType("text");
            e.Property(x => x.ComplexDataJson).HasColumnType("text");
            e.Property(x => x.DomainJson).HasColumnType("text");
            e.HasMany(x => x.Classifications).WithOne(x => x.GuidelineVersion).HasForeignKey(x => x.GuidelineVersionId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.PropertySets).WithOne(x => x.GuidelineVersion).HasForeignKey(x => x.GuidelineVersionId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Properties).WithOne(x => x.GuidelineVersion).HasForeignKey(x => x.GuidelineVersionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GuidelineClassification>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToTable("guideline_classification");
            e.HasIndex(x => new { x.GuidelineVersionId, x.ClassificationId }).IsUnique();
            e.HasIndex(x => new { x.GuidelineVersionId, x.Identifier });
            e.Property(x => x.RelationsJson).HasColumnType("text");
            e.HasMany(x => x.ClassificationProperties).WithOne(x => x.GuidelineClassification).HasForeignKey(x => x.GuidelineClassificationId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GuidelinePropertySet>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToTable("guideline_property_set");
            e.HasIndex(x => new { x.GuidelineVersionId, x.PropertySetId }).IsUnique();
        });

        modelBuilder.Entity<GuidelineProperty>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToTable("guideline_property");
            e.HasIndex(x => new { x.GuidelineVersionId, x.PropertyId }).IsUnique();
            e.Property(x => x.ExtraJson).HasColumnType("text");
        });

        modelBuilder.Entity<GuidelineClassificationProperty>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToTable("guideline_classification_property");
            e.HasIndex(x => new { x.GuidelineClassificationId, x.ClassificationPropertyId }).IsUnique();
            e.Property(x => x.AssignmentJson).HasColumnType("text");
        });
    }
}
