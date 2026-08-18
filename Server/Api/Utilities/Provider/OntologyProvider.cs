// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Data;
using InstanceService.Models.Ontology;
using Microsoft.EntityFrameworkCore;

namespace InstanceService.Api.Utilities.Provider;

public interface IOntologyProvider
{
	Task<IEnumerable<OntologyRelation>> GetRelationsForClassAsync(string classUri, CancellationToken ct = default);

	/// <summary>
	/// All relations across every stored ontology, with domain and range expanded to their concrete
	/// subclasses. Expensive — the expansion is a cross product per relation. Only use it where the
	/// expanded triples are actually needed (relationship validation); for display labels use
	/// <see cref="GetRelationLabelsAsync"/>.
	/// </summary>
	Task<IEnumerable<OntologyRelation>> GetAllRelationsAsync(CancellationToken ct = default);

	/// <summary>
	/// Predicate URI → display label, across every stored ontology. Read straight from the database
	/// without the subclass expansion, because a label depends only on the predicate.
	/// </summary>
	Task<IReadOnlyDictionary<string, string>> GetRelationLabelsAsync(CancellationToken ct = default);
}

public class OntologyDbProvider(
	IServiceProvider serviceProvider,
	ILogger<OntologyDbProvider> logger) : IOntologyProvider
{
	private readonly IServiceProvider _serviceProvider = serviceProvider;
	private readonly ILogger<OntologyDbProvider> _logger = logger;

	public async Task<IEnumerable<OntologyRelation>> GetRelationsForClassAsync(string classUri, CancellationToken ct = default)
	{
		using var scope = _serviceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<InstanceServiceDbContext>();

		var hierarchyEdges = await db.OntologyClassHierarchies
			.ToListAsync(ct);

		var ancestors = ComputeAncestors(classUri, hierarchyEdges);
		var subclassLookup = BuildSubclassLookup(hierarchyEdges);

		// Relations where the class (or any ancestor) is domain OR range
		var rawRelations = await db.OntologyRelations
			.Where(r => (ancestors.Contains(r.DomainUri) || ancestors.Contains(r.RangeUri)))
			.ToListAsync(ct);

		if (rawRelations.Count == 0)
		{
			_logger.LogWarning(
				"No relations found for class {ClassUri}. " +
				"Verify the request sends the fully expanded URI matching the stored Domain/Range URIs.",
				classUri);
		}

		// Expand both sides to concrete subclasses so the client's exact-match filter works
		return rawRelations
			.SelectMany(rel =>
				from d in GetClassWithSubclasses(rel.DomainUri, subclassLookup)
				from r in GetClassWithSubclasses(rel.RangeUri, subclassLookup)
				select new OntologyRelation
				{
					PropertyUri = rel.PropertyUri,
					Label = rel.Label,
					DomainUri = d,
					RangeUri = r
				})
			.DistinctBy(r => (r.PropertyUri, r.DomainUri, r.RangeUri))
			.ToList();
	}

	public async Task<IEnumerable<OntologyRelation>> GetAllRelationsAsync(CancellationToken ct = default)
	{
		using var scope = _serviceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<InstanceServiceDbContext>();

		// Every stored ontology counts, not just the most recently loaded one. Restricting this to the
		// newest version silently dropped the other ontologies — including the case where the class
		// hierarchy and the relations were uploaded as separate files, which left inheritance dead and
		// relation labels unresolvable. This also matches GetRelationsForClassAsync, which never filtered.
		var hierarchyEdges = await db.OntologyClassHierarchies.ToListAsync(ct);
		var rawRelations = await db.OntologyRelations.ToListAsync(ct);

		if (rawRelations.Count == 0)
		{
			_logger.LogWarning("No ontology relations stored in the database");
			return [];
		}

		var subclassLookup = BuildSubclassLookup(hierarchyEdges);

		return rawRelations
			.SelectMany(rel =>
				from d in GetClassWithSubclasses(rel.DomainUri, subclassLookup)
				from r in GetClassWithSubclasses(rel.RangeUri, subclassLookup)
				select new OntologyRelation
				{
					PropertyUri = rel.PropertyUri,
					Label = rel.Label,
					DomainUri = d,
					RangeUri = r
				})
			.DistinctBy(r => (r.PropertyUri, r.DomainUri, r.RangeUri))
			.ToList();
	}

	/// <inheritdoc />
	public async Task<IReadOnlyDictionary<string, string>> GetRelationLabelsAsync(CancellationToken ct = default)
	{
		using var scope = _serviceProvider.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<InstanceServiceDbContext>();

		// Projected and grouped in the query: a label depends on the predicate alone, so neither the
		// hierarchy nor the subclass expansion is needed — and the relation table is large.
		var labels = await db.OntologyRelations
			.Where(r => r.PropertyUri != "" && r.Label != "")
			.Select(r => new { r.PropertyUri, r.Label })
			.Distinct()
			.ToListAsync(ct);

		var labelByUri = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var entry in labels)
		{
			// Same predicate labelled differently in two ontologies: first one wins, deterministically.
			labelByUri.TryAdd(entry.PropertyUri, entry.Label);
		}

		if (labelByUri.Count == 0)
		{
			_logger.LogWarning("No ontology relation labels stored in the database");
		}

		return labelByUri;
	}

	private static HashSet<string> ComputeAncestors(string classUri, IEnumerable<OntologyClassHierarchy> edges)
	{
		var parentLookup = edges
			.GroupBy(e => e.ChildUri)
			.ToDictionary(g => g.Key, g => g.Select(e => e.ParentUri).ToList());

		var ancestors = new HashSet<string> { classUri };
		var queue = new Queue<string>(new[] { classUri });

		while (queue.Count > 0)
		{
			var current = queue.Dequeue();
			if (parentLookup.TryGetValue(current, out var parents))
				foreach (var parent in parents.Where(ancestors.Add))
					queue.Enqueue(parent);
		}

		return ancestors;
	}

	private static Dictionary<string, HashSet<string>> BuildSubclassLookup(IEnumerable<OntologyClassHierarchy> edges)
	{
		var directChildren = edges
			.GroupBy(e => e.ParentUri)
			.ToDictionary(g => g.Key, g => g.Select(e => e.ChildUri).ToHashSet());

		return directChildren.Keys.ToDictionary(
			classUri => classUri,
			classUri => CollectTransitiveSubclasses(classUri, directChildren));
	}

	private static HashSet<string> CollectTransitiveSubclasses(string classUri, Dictionary<string, HashSet<string>> directChildren)
	{
		var result = new HashSet<string>();
		var queue = new Queue<string>();

		if (directChildren.TryGetValue(classUri, out var children))
			foreach (var child in children)
				queue.Enqueue(child);

		while (queue.Count > 0)
		{
			var current = queue.Dequeue();
			if (result.Add(current) && directChildren.TryGetValue(current, out var grandchildren))
				foreach (var child in grandchildren)
					queue.Enqueue(child);
		}

		return result;
	}

	private static IEnumerable<string> GetClassWithSubclasses(string classUri, Dictionary<string, HashSet<string>> subclassLookup)
	{
		yield return classUri;
		if (subclassLookup.TryGetValue(classUri, out var subclasses))
			foreach (var s in subclasses)
				yield return s;
	}
}
