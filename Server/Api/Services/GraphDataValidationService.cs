// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Api.Utilities;
using InstanceService.Api.Utilities.Provider;
using InstanceService.Models;
using InstanceService.Models.Ontology;
using VDS.RDF;
using VDS.RDF.Parsing;

namespace InstanceService.Api.Services;

/// <summary>
/// Implements the <see cref="IGraphDataModelValidationService"/> to validate a <see cref="GraphDataModel"/>.
/// </summary>
public class GraphDataModelValidationService : IGraphDataModelValidationService
{
    private readonly IGuidelineProvider _guidelineProvider;
    private readonly IOntologyProvider _ontologyProvider;
    private readonly ILogger<GraphDataModelValidationService> _logger;
    private readonly IAccessRightHelper _accessRightHelper;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphDataModelValidationService"/> class.
    /// </summary>
    /// <param name="guidelineProvider">The provider for accessing guideline data.</param>
    /// <param name="ontologyProvider">The provider for accessing ontology data.</param>
    /// <param name="logger">The logger for recording validation events and errors.</param>
    /// <param name="accessRightHelper">The helper for validating access rights.</param>
    public GraphDataModelValidationService(
        IGuidelineProvider guidelineProvider,
        IOntologyProvider ontologyProvider,
        ILogger<GraphDataModelValidationService> logger,
        IAccessRightHelper accessRightHelper)
    {
        _guidelineProvider = guidelineProvider;
        _ontologyProvider = ontologyProvider;
        _logger = logger;
        _accessRightHelper = accessRightHelper;
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAsync(GraphDataModel model)
    {
        var result = new ValidationResult();

        try
        {
            var guideline = await _guidelineProvider.GetGuideline(model.UseCase.Id);
            var validClassTypes = guideline.Domain.Classifications
                .Select(c => c.Identifier.ToString())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (model != null)
            {
                var filteredNodes = model.GraphMetadata.Where(node => !string.IsNullOrEmpty(node.ClassType) && !validClassTypes.Contains(node.ClassType));
                foreach (var node in filteredNodes)
                {
                    var error = $"Invalid classtype '{node.ClassType}' for node '{node.Id}'. Must match a classification from the guideline.";
                    result.AddError(error);

                    _logger.LogWarning("GraphDataModel validation error: {ValidationErrorType} for node {NodeId} with classType {ClassType}",
                        "InvalidClassType", node.Id, node.ClassType);
                }
            }

            var relationshipValidation = await ValidateRelationshipsAsync(model);
            result.MergeErrors(relationshipValidation);

            _logger.LogInformation("GraphDataModel validation completed with {ErrorCount} errors. NodeCount: {NodeCount}",
                result.Errors.Count, model?.GraphMetadata?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating GraphDataModel");
            result.AddError("Internal validation error occurred");
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateRelationshipsAsync(GraphDataModel model)
    {
        var result = new ValidationResult();

        try
        {
            if (model?.GraphData == null || string.IsNullOrWhiteSpace(model.GraphData))
            {
                _logger.LogInformation("No graph data to validate relationships");
                return result;
            }

            var graph = new Graph();
            graph.LoadFromString(model.GraphData, new TurtleParser());

            var validRelationships = await GetValidRelationshipsFromOntologyAsync();

            foreach (var triple in graph.Triples)
            {
                if (triple.Subject is IUriNode subjectNode &&
                    triple.Object is IUriNode objectNode &&
                    triple.Predicate is IUriNode predicateNode)
                {
                    var isValidRelationship = ValidateRelationship(
                        subjectNode, predicateNode, objectNode,
                        model.GraphMetadata, validRelationships);

                    if (!isValidRelationship)
                    {
                        result.AddError($"Invalid relationship: {predicateNode.Uri} between {subjectNode.Uri} and {objectNode.Uri}. This relationship is not allowed by the ontology.");
                    }
                }
            }

            _logger.LogInformation("Validated {TripleCount} relationships with {ErrorCount} errors",
                graph.Triples.Count, result.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating relationships");
            result.AddError("Internal relationship validation error occurred");
        }

        return result;
    }

    private async Task<HashSet<(string subjectClass, string predicate, string objectClass)>> GetValidRelationshipsFromOntologyAsync()
    {
        var relations = await _ontologyProvider.GetAllRelationsAsync();
        return relations
            .Select(r => (r.DomainUri, r.PropertyUri, r.RangeUri))
            .ToHashSet();
    }

    /// <summary>
    /// Validates a single relationship (3-tuple) against the set of allowed relationships.
    /// </summary>
    /// <param name="subjectNode">The subject node of the 3-tuple.</param>
    /// <param name="predicateNode">The predicate node of the 3-tuple.</param>
    /// <param name="objectNode">The object node of the 3-tuple.</param>
    /// <param name="metadata">The list of metadata nodes to look up class types.</param>
    /// <param name="validRelationships">The set of valid relationships from the ontology.</param>
    /// <returns><see langword="true"/> if the relationship is valid; otherwise, <see langword="false"/>.</returns>
    private bool ValidateRelationship(
        IUriNode subjectNode,
        IUriNode predicateNode,
        IUriNode objectNode,
        List<MetaDataNode> metadata,
        HashSet<(string subjectClass, string predicate, string objectClass)> validRelationships)
    {
        var subjectId = ExtractNodeId(subjectNode.Uri.ToString());
        var objectId = ExtractNodeId(objectNode.Uri.ToString());

        var subjectMetadata = metadata.FirstOrDefault(m => m.Id == subjectId);
        var objectMetadata = metadata.FirstOrDefault(m => m.Id == objectId);

        if (subjectMetadata == null || objectMetadata == null)
        {
            _logger.LogWarning("Could not find metadata for subject {SubjectId} or object {ObjectId}", subjectId, objectId);
            return false;
        }

        var predicateUri = predicateNode.Uri.ToString();

        return validRelationships.Contains((subjectMetadata.ClassType, predicateUri, objectMetadata.ClassType));
    }

    /// <summary>
    /// Extracts the node ID from a given URI.
    /// </summary>
    /// <param name="uri">The URI to parse.</param>
    /// <returns>The last segment of the URI path, or an empty string if the URI is invalid.</returns>
    private static string ExtractNodeId(string uri)
    {
        return uri.Split('/').LastOrDefault() ?? string.Empty;
    }

    /// <inheritdoc/>
    public async Task<ValidationResult> ValidateAccessRightsAsync(GraphDataModel model, List<string> groupIds, string useCaseId, IEnumerable<AccessRight> accessRights)
    {
        var result = new ValidationResult();

        try
        {
            if (model?.GraphMetadata == null || !model.GraphMetadata.Any())
            {
                _logger.LogInformation("No graph metadata to validate access rights for");
                return result;
            }

            _logger.LogInformation("Starting access rights validation for {NodeCount} nodes with useCase {UseCaseId} and {GroupCount} groups",
                model.GraphMetadata.Count, useCaseId, groupIds.Count);

            foreach (var node in model.GraphMetadata)
            {
                if (!_accessRightHelper.CanCreate(node.ClassType, groupIds, accessRights, useCaseId))
                {
                    var error = $"Insufficient access rights to create instance of type '{node.ClassType}' for node '{node.Id}' in use case '{useCaseId}'";
                    result.AddError(error);

                    _logger.LogWarning("Access rights validation error: {ValidationErrorType} for node {NodeId} with classType {ClassType} in useCase {UseCaseId}",
                        "InsufficientCreateRights", node.Id, node.ClassType, useCaseId);
                }

                if (node.PropertiesValues?.Any() == true)
                {
                    foreach (var property in node.PropertiesValues)
                    {
                        if (!_accessRightHelper.CanUpdate(node.ClassType, groupIds, accessRights, useCaseId, property.Key))
                        {
                            var error = $"Insufficient access rights to set property '{property.Key}' on instance of type '{node.ClassType}' for node '{node.Id}' in use case '{useCaseId}'";
                            result.AddError(error);

                            // Structured logging for property rights validation errors
                            _logger.LogWarning("Access rights validation error: {ValidationErrorType} for node {NodeId} property {PropertyKey} with classType {ClassType} in useCase {UseCaseId}",
                                "InsufficientPropertyRights", node.Id, property.Key, node.ClassType, useCaseId);
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(model.GraphData))
            {
                var relationValidation = ValidateRelationAccessRights(model, groupIds, useCaseId, accessRights);
                result.MergeErrors(relationValidation);
            }

            _logger.LogInformation("Access rights validation completed with {ErrorCount} errors", result.Errors.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating access rights for GraphDataModel");
            result.AddError("Internal access rights validation error occurred");
        }

        return result;
    }

    /// <summary>
    /// Validates the access rights for creating relations defined in the graph data.
    /// </summary>
    /// <param name="model">The graph data model containing the relations.</param>
    /// <param name="groupIds">The list of user group IDs.</param>
    /// <param name="useCaseId">The ID of the current use case.</param>
    /// <param name="accessRights">The collection of access rights to check against.</param>
    /// <returns>A <see cref="ValidationResult"/> with any errors found during relation access rights validation.</returns>
    private ValidationResult ValidateRelationAccessRights(GraphDataModel model, List<string> groupIds, string useCaseId, IEnumerable<AccessRight> accessRights)
    {
        var result = new ValidationResult();

        try
        {
            var graph = new Graph();
            graph.LoadFromString(model.GraphData, new TurtleParser());

            foreach (var triple in graph.Triples)
            {
                if (triple.Subject is IUriNode subjectNode && triple.Object is IUriNode objectNode)
                {
                    var subjectId = ExtractNodeId(subjectNode.Uri.ToString());
                    var objectId = ExtractNodeId(objectNode.Uri.ToString());

                    var subjectMetadata = model.GraphMetadata.FirstOrDefault(m => m.Id == subjectId);
                    var objectMetadata = model.GraphMetadata.FirstOrDefault(m => m.Id == objectId);

                    if (subjectMetadata != null && objectMetadata != null)
                    {
                        if (!_accessRightHelper.CanCreateRelations(
                            subjectMetadata.ClassType,
                            objectMetadata.ClassType,
                            groupIds,
                            accessRights,
                            useCaseId))
                        {
                            result.AddError($"Insufficient access rights to create relation between '{subjectMetadata.ClassType}' and '{objectMetadata.ClassType}' in use case '{useCaseId}'");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating relation access rights");
            result.AddError("Error validating relation access rights");
        }

        return result;
    }

}

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets the list of validation errors. An empty list indicates a successful validation.
    /// </summary>
    public List<string> Errors { get; } = new List<string>();

    /// <summary>
    /// Gets a value indicating whether the validation was successful (i.e., there are no errors).
    /// </summary>
    /// <value><see langword="true"/> if no errors were found; otherwise, <see langword="false"/>.</value>
    public bool IsValid => !Errors.Any();

    /// <summary>
    /// Adds an error message to the validation result.
    /// </summary>
    /// <param name="error">The error message to add.</param>
    public void AddError(string error)
    {
        Errors.Add(error);
    }

    /// <summary>
    /// Merges the errors from another validation result into this one.
    /// </summary>
    /// <param name="other">The <see cref="ValidationResult"/> to merge errors from.</param>
    public void MergeErrors(ValidationResult other)
    {
        Errors.AddRange(other.Errors);
    }
}