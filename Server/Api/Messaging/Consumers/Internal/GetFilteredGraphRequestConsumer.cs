// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Api.Messaging.Consumers.Internal.Contracts;
using InstanceService.Api.Utilities;
using InstanceService.Api.Utilities.Provider;
using InstanceService.Data;
using InstanceService.Domain.IRepositories;
using InstanceService.Models.Enum;
using Messaging.Core.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using InstanceService.Models;
using System.Linq.Dynamic.Core;
using static InstanceService.Api.Utilities.CypherToLinqTranslator;

namespace InstanceService.Api.Messaging.Consumers.Internal
{
    /// <summary>
    /// Consumer that processes incoming <see cref="GetFilteredGraphRequest"/> messages,
    /// filters the instances and relations according to access rights and specified query,
    /// and returns a structured <see cref="GetFilteredGraphResponse"/>.
    /// </summary>
    public class GetFilteredGraphRequestConsumer : IInternalRequestConsumer<GetFilteredGraphRequest, GetFilteredGraphResponse>
    {
        public ILogger<IInternalRequestConsumer<GetFilteredGraphRequest, GetFilteredGraphResponse>> Logger { get; }

        private readonly IInstanceRepository _repository;
        private readonly IAccessRightsFetcher _accessRightsFetcher;
        private readonly InstanceServiceDbContext _context;
        private readonly IAccessRightHelper _accessRightHelper;
        private readonly IUserGroupProvider _userGroupProvider;
        private readonly ICypherToLinqTranslator _cypherTranslator;
        private readonly IOntologyProvider _ontologyProvider;

        public GetFilteredGraphRequestConsumer(
            ILogger<IInternalRequestConsumer<GetFilteredGraphRequest, GetFilteredGraphResponse>> logger,
            IInstanceRepository repository,
            IAccessRightsFetcher accessRightsFetcher,
            InstanceServiceDbContext dbContext,
            IAccessRightHelper accessRightHelper,
            IUserGroupProvider userGroupProvider,
            ICypherToLinqTranslator cypherTranslator,
            IOntologyProvider ontologyProvider)
        {
            Logger = logger;
            _repository = repository;
            _accessRightsFetcher = accessRightsFetcher;
            _context = dbContext;
            _accessRightHelper = accessRightHelper;
            _userGroupProvider = userGroupProvider;
            _cypherTranslator = cypherTranslator;
            _ontologyProvider = ontologyProvider;
        }

        /// <summary>
        /// Processes a <see cref="GetFilteredGraphRequest"/> message, applies filtering,
        /// access rights logic, and returns the resulting graph.
        /// </summary>
        /// <param name="request">The request containing query, token and use case data.</param>
        /// <returns>Response containing filtered instances and relations.</returns>
        public async Task<GetFilteredGraphResponse> ConsumeInternal(GetFilteredGraphRequest request)
        {
            IEnumerable<Models.Instance> instances = await _repository.GetInstances();
            IEnumerable<AccessRight> accessRights = await _accessRightsFetcher.GetAccessRightsAsync();

            var filteredInstances = PreFilterInstancesByRelevance(instances, accessRights, request.UseCaseId);
            var filteredRelations = PreFilterRelationsByRelevance(filteredInstances, accessRights, request.UseCaseId);

            await TryFetchMetadata(filteredInstances, request.Token, request.UseCaseId, accessRights);

            var queryResult = TryApplyFilter(request.TextQuery, filteredInstances, filteredRelations);

            var relationsList = queryResult.DetailedRelations.ToList();

            filteredInstances = FilterInstances(relationsList, queryResult.TranslationResult);

            var instanceTuples = CreateInstanceTuples(filteredInstances, accessRights, request.UseCaseId);

            var responseRelations = queryResult.TranslationResult.ReturnContainsPredicate
                ? relationsList.Select(relation => new Models.InstanceRelation
                {
                    SubjectId = relation.SubjectInstance != null ? relation.SubjectInstance.Id : string.Empty,
                    PredicateUri = relation.Predicate != null ? relation.Predicate.Label : string.Empty,
                    ObjectId = relation.ObjectInstance != null ? relation.ObjectInstance.Id : string.Empty
                }).ToList()
                : new List<Models.InstanceRelation>();

            await EnrichRelationLabelsAsync(responseRelations);

            GetFilteredGraphResponse response = new()
            {
                Instances = instanceTuples.ToList(),
                Relations = responseRelations
            };
            return response;
        }

        /// <summary>
        /// Sets the optional display <see cref="Models.InstanceRelation.Label"/> on each relation from the
        /// ontology, keyed by its predicate URI. Relations stay identified by URI; the label is display-only.
        /// </summary>
        private async Task EnrichRelationLabelsAsync(IEnumerable<Models.InstanceRelation> relations)
        {
            var labelByUri = await _ontologyProvider.GetRelationLabelsAsync();

            foreach (var relation in relations)
            {
                if (!string.IsNullOrEmpty(relation.PredicateUri) && labelByUri.TryGetValue(relation.PredicateUri, out var label))
                    relation.Label = label;
            }
        }

        /// <summary>
        /// Filters instances based on access rights and usecaseId for relevance.
        /// </summary>
        /// <param name="instances">All instances.</param>
        /// <param name="accessRights">All access rights.</param>
        /// <param name="useCaseId">Usecase identifier.</param>
        /// <returns>Relevant instances to the current request/user.</returns>
        private List<Models.Instance> PreFilterInstancesByRelevance(
            IEnumerable<Models.Instance> instances, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            var relevantInstanceIds = accessRights
                .Where(accessRight => accessRight.UseCaseId.ToString() == useCaseId)
                .Select(accessRight => accessRight.GuidelineClassificationId)
                .Distinct();

            return instances
                .Where(instance => relevantInstanceIds.Contains(instance.ClassificationId))
                .ToList();
        }

        /// <summary>
        /// Filters relations to only those pertinent for the filtered instances.
        /// </summary>
        /// <param name="instances">Filtered instances.</param>
        /// <param name="accessRights">Access rights.</param>
        /// <param name="useCaseId">Usecase identifier.</param>
        /// <returns>Relations relevant for response.</returns>
        private List<Models.InstanceRelation> PreFilterRelationsByRelevance(
            IEnumerable<Models.Instance> instances, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            IEnumerable<Models.InstanceRelation> allRelations = instances.SelectMany(i => i.Relations).Distinct();
            var instanceIds = instances.Select(instance => instance.Id).ToHashSet();

            return allRelations
                .Where(relation => instanceIds.Contains(relation.SubjectId) || instanceIds.Contains(relation.ObjectId))
                .ToList();
        }

        /// <summary>
        /// Translates the provided Cypher query to Linq, applies it on instances-relations.
        /// </summary>
        /// <param name="textQuery">Cypher-style query string.</param>
        /// <param name="filteredInstances">Prefiltered instances.</param>
        /// <param name="filteredRelations">Prefiltered relations.</param>
        /// <returns>Packaged query result with translation information.</returns>
        private QueryResult TryApplyFilter(
            string textQuery, List<Models.Instance> filteredInstances, List<Models.InstanceRelation> filteredRelations)
        {
            var translationResult = _cypherTranslator.InterpreteCypher(textQuery, new CypherToLinqTranslator.RelationVariableNames
            {
                Subject = nameof(DetailedRelation.SubjectInstance),
                Predicate = nameof(DetailedRelation.Predicate),
                Object = nameof(DetailedRelation.ObjectInstance)
            });

            var detailedRelations = filteredRelations
                .Select(relation => new DetailedRelation
                {
                    SubjectInstance = filteredInstances.Where(instance => instance.Id == relation.SubjectId).SingleOrDefault(new Models.Instance()),
                    // ReducedRelation.Label carries the predicate URI here; the name stays "Label"
                    // because the Cypher query DSL references the predicate via `r.Label`.
                    Predicate = new ReducedRelation { Label = relation.PredicateUri },
                    ObjectInstance = filteredInstances.Where(instance => instance.Id == relation.ObjectId).SingleOrDefault(new Models.Instance())
                }).AsQueryable();

            try
            {
                detailedRelations = detailedRelations.Where(translationResult.LinqWhere);
                Logger.LogInformation($"Execution of Users WHERE query as Linq: {translationResult.LinqWhere}");
            }
            catch (Exception ex)
            {
                throw new OperationCanceledException($"Interpreting Cypher Query failed: {ex}");
            }

            return new QueryResult
            {
                TranslationResult = translationResult,
                DetailedRelations = detailedRelations
            };
        }

        /// <summary>
        /// Attempts to load and assign metadata for each instance, depending on user rights.
        /// </summary>
        /// <param name="filteredInstances">Instances filtered for relevance.</param>
        /// <param name="token">Authentication token.</param>
        /// <param name="useCaseId">Current usecase identifier.</param>
        /// <param name="accessRights">Collection of access rights.</param>
        private async Task TryFetchMetadata(
            List<Models.Instance> filteredInstances,
            string token,
            string useCaseId,
            IEnumerable<AccessRight> accessRights)
        {
            foreach (var instance in filteredInstances)
            {
                Models.InstanceMetaData? metadata = _context.InstanceMetadata
                    .Where(x => x.Id == instance.Id)
                    .FirstOrDefault();

                var classificationId = instance.ClassificationId;
                var groupIds = await _userGroupProvider.GetUserGroupIdsAsync(token);
                var canGetMetadata = _accessRightHelper.CanGetMetadata(classificationId, groupIds, accessRights, useCaseId);

                if (canGetMetadata)
                    instance.Properties = metadata == null ? [] : metadata.Properties;
            }
        }

        /// <summary>
        /// Determines the correct <see cref="Accessibility"/> for each instance,
        /// based on the available property rights for the usecase.
        /// </summary>
        /// <param name="filteredInstances">Instances after filtering.</param>
        /// <param name="accessRights">Available access rights.</param>
        /// <param name="useCaseId">Use case identifier.</param>
        /// <returns>Tuple of instance and its accessibility.</returns>
        private List<(Models.Instance, Accessibility)> CreateInstanceTuples(
            List<Models.Instance> filteredInstances, IEnumerable<AccessRight> accessRights, string useCaseId)
        {
            var instanceTuples = new List<(Models.Instance, Accessibility)>();
            foreach (var instance in filteredInstances)
            {
                var propertyRights = accessRights
                    .Where(accessRight => accessRight.GuidelineClassificationId == instance.ClassificationId)
                    .Where(accessRight => accessRight.UseCaseId.ToString() == useCaseId)
                    .Select(accessRight => accessRight.Right)
                    .ToList();

                Accessibility accessibility;
                if (propertyRights.Any() && propertyRights.All(right => right == PropertyRight.Write))
                {
                    accessibility = Accessibility.FullControl;
                }
                else if (propertyRights.Any(right => right == PropertyRight.Read) && propertyRights.Any(right => right == PropertyRight.Write))
                {
                    accessibility = Accessibility.ReadWrite;
                }
                else if (propertyRights.Any(right => right == PropertyRight.Read) && propertyRights.All(right => right != PropertyRight.Write))
                {
                    accessibility = Accessibility.ReadOnly;
                }
                else
                {
                    accessibility = Accessibility.None;
                }
                instanceTuples.Add((instance, accessibility));
            }
            return instanceTuples;
        }

        /// <summary>
        /// Applies the result of Cypher-translation to filter instances (e.g. subject/object selection).
        /// </summary>
        /// <param name="relationsList">List of processed relations.</param>
        /// <param name="translationResult">Translation outcome indicates which entity to contain.</param>
        /// <returns>Instances for the response.</returns>
        private List<Models.Instance> FilterInstances(List<DetailedRelation> relationsList, TranslationResult translationResult)
        {
            foreach (var relation in relationsList)
            {
                relation.SubjectInstance = translationResult.ReturnContainsSubject
                    ? relation.SubjectInstance
                    : new Models.Instance();

                relation.Predicate = translationResult.ReturnContainsPredicate
                    ? relation.Predicate
                    : new ReducedRelation();

                relation.ObjectInstance = translationResult.ReturnContainsObject
                    ? relation.ObjectInstance
                    : new Models.Instance();
            }
            // Gather all (filtered) subject and object instances, remove empty and duplicates
            return relationsList
                .SelectMany(relation => new[] { relation.SubjectInstance, relation.ObjectInstance })
                .OfType<Models.Instance>()
                .Where(instance => !instance.ClassificationId.IsNullOrEmpty())
                .DistinctBy(instance => instance.Id)
                .ToList();
        }

        /// <summary>
        /// Helper class that models a fully decorated relation for query processing.
        /// </summary>
        private class DetailedRelation
        {
            /// <summary>
            /// Gets or sets the subject instance of the relation.
            /// </summary>
            public Models.Instance SubjectInstance { get; set; } = new Models.Instance();

            /// <summary>
            /// Gets or sets the reduced predicate.
            /// </summary>
            public ReducedRelation Predicate { get; set; } = new ReducedRelation();

            /// <summary>
            /// Gets or sets the object instance of the relation.
            /// </summary>
            public Models.Instance ObjectInstance { get; set; } = new Models.Instance();
        }

        /// <summary>
        /// Helper class that models a simplified predicate for filtering.
        /// </summary>
        private class ReducedRelation
        {
            /// <summary>
            /// Gets or sets the label of the predicate.
            /// </summary>
            public string Label { get; set; } = string.Empty;
        }

        /// <summary>
        /// Helper class to group the query result from cypher translation and filter application.
        /// </summary>
        private class QueryResult
        {
            /// <summary>
            /// Gets or sets the resulting detailed relations after query/filter.
            /// </summary>
            public IEnumerable<DetailedRelation> DetailedRelations { get; set; } = [];

            /// <summary>
            /// Gets or sets the cypher-to-linq translation outcomes.
            /// </summary>
            public TranslationResult TranslationResult { get; set; } = new TranslationResult();
        }
    }
}
