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

namespace InstanceService.Domain.IRepositories;

/// <summary>
/// Defines the repository for managing instances and their relationships.
/// </summary>
public interface IInstanceRepository
{

    /// <summary>
    /// Gets all instances asynchronously.
    /// </summary>
    /// <param name="withMetadata">A flag to indicate whether to include metadata for the instances. The default is false.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of all instances.</returns>
    public Task<IEnumerable<Instance>> GetInstances(bool withMetadata = false);

    /// <summary>
    /// Gets a specific instance by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the instance.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the instance if found; otherwise, <c>null</c>.</returns>
    public Task<Instance?> GetInstance(string id);

    /// <summary>
    /// Updates an existing instance with the specified ID.
    /// </summary>
    /// <param name="id">The unique identifier of the instance to update.</param>
    /// <param name="name">The new name for the instance.</param>
    /// <param name="data">The new data to associate with the instance.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task UpdateInstance(string id, string name, Dictionary<string, string> data);

    /// <summary>
    /// Create a new instance with given name, classificationId and data.
    /// </summary>
    /// <param name="name">The name of the instance.</param>
    /// <param name="classificationId">The classification id of the instance.</param>
    /// <param name="data">The data of the instance.</param>
    /// <param name="id">The id of the instance.</param>
    public Task CreateInstance(string name, string classificationId, Dictionary<string, string> data, string id);

    /// <summary>
    /// Create a new instance with given name, classificationId and data.
    /// </summary>
    /// <param name="name">The name of the instance.</param>
    /// <param name="classificationId">The classification id of the instance.</param>
    /// <param name="data">The data of the instance.</param>
    /// <returns>Id of the created instance.</returns>
    public Task<string> CreateInstance(string name, string classificationId, Dictionary<string, string> data);

    /// <summary>
    /// Delete the instance with the given id with all relations.
    /// </summary>
    /// <param name="id">The id of the node.</param>
    public Task DeleteInstance(string id);

    /// <summary>
    /// Deletes every instance (graph vertex with all its relations, plus its metadata) whose
    /// classification is in <paramref name="classificationIds"/>. Used when a guideline change or
    /// deletion removes classifications: instances of classes that no longer exist are dropped.
    /// </summary>
    /// <param name="classificationIds">The classification IDs whose instances should be deleted.</param>
    /// <returns>The number of instances deleted.</returns>
    public Task<int> DeleteInstancesByClassificationIds(IEnumerable<string> classificationIds);

    /// <summary>
    /// Delete all relations of the instance with the given id.
    /// </summary>
    /// <param name="id">The id of the node.</param>
    public Task DeleteRelations(string id);

    /// <summary>
    /// Delete a specific relation between two instances, identified by subject, object and predicate URI.
    /// Only the edge whose type matches <paramref name="predicateUri"/> is removed; other relations
    /// between the same pair are left intact.
    /// </summary>
    /// <param name="subjectId">The id of the subject node.</param>
    /// <param name="objectId">The id of the object node.</param>
    /// <param name="predicateUri">The canonical ontology property URI identifying the relation (the edge type).</param>
    /// <returns></returns>
    public Task DeleteRelation(string subjectId, string objectId, string predicateUri);

    /// <summary>
    /// Creates a single relationship with a label between a subject and an object instance.
    /// </summary>
    /// <param name="subjectId">The unique identifier of the subject instance.</param>
    /// <param name="objectId">The unique identifier of the object instance.</param>
    /// <param name="predicateUri">The canonical ontology property URI identifying the relationship.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task CreateRelation(string subjectId, string objectId, string predicateUri) => CreateRelations((subjectId, objectId, predicateUri));

    /// <summary>
    /// Creates multiple relationships based on a collection of 3-tuples.
    /// </summary>
    /// <param name="relations">An enumerable of 3-tuples, each representing a relationship with a subject ID, an object ID, and a predicate URI.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task CreateRelation(IEnumerable<(string subjectId, string objectId, string predicateUri)> relations) => CreateRelations(relations.ToArray());

    /// <summary>
    /// Creates one or more relationships from a parameter array of 3-tuples.
    /// </summary>
    /// <param name="relations">A parameter array of 3-tuples, each representing a relationship to be created.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task CreateRelations(params (string subjectId, string objectId, string predicateUri)[] relations)
        => CreateRelations(
            relations.Select(relation =>
                new InstanceRelation()
                {
                    SubjectId = relation.subjectId,
                    ObjectId = relation.objectId,
                    PredicateUri = relation.predicateUri
                }));

    /// <summary>
    /// Creates multiple relationships from a collection of <see cref="InstanceRelation"/> objects.
    /// </summary>
    /// <param name="relations">An enumerable of <see cref="InstanceRelation"/> objects to be created.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task CreateRelations(IEnumerable<InstanceRelation> relations) => CreateRelations(relations.ToArray());

    /// <summary>
    /// Creates multiple relationships from a parameter array of <see cref="InstanceRelation"/> objects.
    /// </summary>
    /// <param name="relations">A parameter array of <see cref="InstanceRelation"/> objects, each representing a relationship to be created.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task CreateRelations(params InstanceRelation[] relations);

    /// <summary>
    /// Create new instances based on an Enumerable of MetaDataNodes.
    /// If an instanceId already exists, this instance will be updated.
    /// </summary>
    /// <param name="newInstances">The MetaData of the new instances.</param>
    /// <param name="name">The name of all instances.</param>
    /// <returns>IdMap of the eventually updated or created instances.</returns>
    public Task<Dictionary<string, string>> UpsertInstances(IEnumerable<MetaDataNode> newInstances, string name);
}
