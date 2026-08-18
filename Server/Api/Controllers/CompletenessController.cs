// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

//using InstanceService.Api.Utilities.Interfaces;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Swashbuckle.AspNetCore.Annotations;

//namespace InstanceService.Api.Controllers;

//[Route("api/[controller]")]
//[ProducesResponseType(StatusCodes.Status401Unauthorized)]
//[Authorize]
//public class CompletenessController : ControllerBase
//{
//    private readonly ICompletenessCheck _completenessCheck;
//    private readonly ILogger<CompletenessController> _logger;

//    public CompletenessController(
//     ICompletenessCheck completenessCheck,
//        ILogger<CompletenessController> logger)
//    {
//        _completenessCheck = completenessCheck;
//        _logger = logger;
//    }

//    /// <summary>
//    /// Checks if a graph is complete for a specific use case starting from a given instance.
//    /// </summary>
//    /// <param name="useCaseId">The ID of the use case to check</param>
//    /// <param name="instanceId">The ID of the starting instance</param>
//    /// <returns>True if the graph is complete for the use case, false otherwise</returns>
//    /// <response code="200">Returns the completeness status</response>
//    /// <response code="400">If the parameters are invalid</response>
//    [HttpGet("use-case/{useCaseId}/instance/{instanceId}")]
//    [ProducesResponseType(typeof(CompletenessCheckResult), 200)]
//    [ProducesResponseType(400)]
//    [SwaggerOperation(
//          Summary = "Check graph completeness from instance",
//          Description = "Checks if a graph starting from a specific instance is complete for a use case",
//          Tags = new[] { "Completeness" }
//      )]
//    public async Task<IActionResult> CheckCompletenessFromInstance(string useCaseId, string instanceId)
//    {
//        if (string.IsNullOrEmpty(useCaseId) || string.IsNullOrEmpty(instanceId))
//        {
//            return BadRequest("UseCaseId and InstanceId must be provided");
//        }

//        try
//        {
//            _logger.LogInformation("Checking completeness for use case {UseCaseId} from instance {InstanceId}", useCaseId, instanceId);

//            var isComplete = await _completenessCheck.IsUseCaseCompleteAsync(instanceId, useCaseId);

//            var result = new CompletenessCheckResult
//            {
//                UseCaseId = useCaseId,
//                StartInstanceId = instanceId,
//                IsComplete = isComplete,
//                CheckedAt = DateTime.UtcNow
//            };

//            return Ok(result);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error checking completeness for use case {UseCaseId} from instance {InstanceId}", useCaseId, instanceId);
//            return StatusCode(500, $"Error checking completeness: {ex.Message}");
//        }
//    }

//    /// <summary>
//    /// Checks all use cases for completeness starting from a given instance and sends messages for complete ones.
//    /// </summary>
//    /// <param name="instanceId">The ID of the starting instance</param>
//    /// <returns>No content</returns>
//    /// <response code="204">Check completed and messages sent for complete use cases</response>
//    /// <response code="400">If the instance ID is invalid</response>
//    [HttpPost("instance/{instanceId}")]
//    [ProducesResponseType(204)]
//    [ProducesResponseType(400)]
//    [SwaggerOperation(
// Summary = "Check and send for all use cases from instance",
// Description = "Checks all use cases for completeness starting from a specific instance and sends Kafka messages for complete ones",
//        Tags = new[] { "Completeness" }
//    )]
//    public async Task<IActionResult> CheckAndSendFromInstance(string instanceId)
//    {
//        if (string.IsNullOrEmpty(instanceId))
//        {
//            return BadRequest("InstanceId must be provided");
//        }

//        try
//        {
//            _logger.LogInformation("Checking all use cases from instance {InstanceId}", instanceId);

//            await _completenessCheck.CheckAndSendAsync(instanceId);

//            return NoContent();
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error checking all use cases from instance {InstanceId}", instanceId);
//            return StatusCode(500, $"Error checking use cases: {ex.Message}");
//        }
//    }

//    /// <summary>
//    /// Finds all complete subgraphs for a specific use case without requiring a start instance.
//    /// Sends a separate Kafka message for each complete subgraph found.
//    /// </summary>
//    /// <param name="useCaseId">The ID of the use case to check</param>
//    /// <returns>List of root instance IDs that form complete subgraphs</returns>
//    /// <response code="200">Returns the list of root instances for complete subgraphs</response>
//    /// <response code="400">If the use case ID is invalid</response>
//    [HttpPost("use-case/{useCaseId}")]
//    [ProducesResponseType(typeof(CompleteSubgraphsResult), 200)]
//    [ProducesResponseType(400)]
//    [SwaggerOperation(
//        Summary = "Find and send complete subgraphs for use case",
//     Description = "Finds all complete subgraphs for a use case without requiring a start instance. Sends a Kafka message for each complete subgraph.",
//        Tags = new[] { "Completeness" }
//    )]
//    public async Task<IActionResult> FindAndSendCompleteSubgraphs(string useCaseId)
//    {
//        if (string.IsNullOrEmpty(useCaseId))
//        {
//            return BadRequest("UseCaseId must be provided");
//        }

//        try
//        {
//            _logger.LogInformation("Finding complete subgraphs for use case {UseCaseId}", useCaseId);

//            var rootInstanceIds = await _completenessCheck.FindAndSendCompleteSubgraphsAsync(useCaseId);

//            var result = new CompleteSubgraphsResult
//            {
//                UseCaseId = useCaseId,
//                CompleteSubgraphCount = rootInstanceIds.Count,
//                RootInstanceIds = rootInstanceIds,
//                CheckedAt = DateTime.UtcNow
//            };

//            _logger.LogInformation("Found {Count} complete subgraphs for use case {UseCaseId}",
//         rootInstanceIds.Count, useCaseId);

//            return Ok(result);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error finding complete subgraphs for use case {UseCaseId}", useCaseId);
//            return StatusCode(500, $"Error finding complete subgraphs: {ex.Message}");
//        }
//    }

//    /// <summary>
//    /// Checks all use cases globally and sends messages for each complete subgraph found.
//    /// Does not require a start instance. This can be a long-running operation.
//    /// </summary>
//    /// <returns>Summary of the global check</returns>
//    /// <response code="200">Returns summary of complete subgraphs found across all use cases</response>
//    [HttpPost("all-use-cases")]
//    [ProducesResponseType(typeof(GlobalCompletenessResult), 200)]
//    [SwaggerOperation(
//      Summary = "Check and send for all use cases globally",
//        Description = "Checks all use cases globally without requiring a start instance. Finds and sends messages for all complete subgraphs. This can be a long-running operation.",
//        Tags = new[] { "Completeness" }
//    )]
//    public async Task<IActionResult> CheckAndSendAllUseCasesGlobally()
//    {
//        try
//        {
//            _logger.LogInformation("Starting global completeness check for all use cases");
//            var startTime = DateTime.UtcNow;

//            var useCaseMap = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();

//            // Get all use cases and check them
//            var allUseCases = await _completenessCheck.GetAllUseCaseIdsAsync();

//            foreach (var useCaseId in allUseCases)
//            {
//                var subgraphs = await _completenessCheck.FindAndSendCompleteSubgraphsAsync(useCaseId);
//                useCaseMap[useCaseId] = subgraphs.Count;
//            }

//            var endTime = DateTime.UtcNow;
//            var duration = endTime - startTime;
//            var totalSubgraphs = useCaseMap.Values.Sum();

//            var result = new GlobalCompletenessResult
//            {
//                CheckStartedAt = startTime,
//                CheckCompletedAt = endTime,
//                DurationSeconds = duration.TotalSeconds,
//                TotalSubgraphs = totalSubgraphs,
//                Message = $"Global completeness check completed. Found {totalSubgraphs} complete subgraphs across {useCaseMap.Count} use cases."
//            };

//            _logger.LogInformation("Global check completed. Found {TotalCount} complete subgraphs in {Duration} seconds",
//     totalSubgraphs, duration.TotalSeconds);

//            return Ok(result);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error during global completeness check");
//            return StatusCode(500, $"Error during global completeness check: {ex.Message}");
//        }
//    }

//    /// <summary>
//    /// Checks all use cases for completeness for multiple instances and sends messages for complete ones.
//    /// Automatically handles duplicate subgraphs to avoid sending duplicate messages.
//    /// </summary>
//    /// <param name="request">Request containing an array of instance IDs to check</param>
//    /// <returns>Summary of processed instances</returns>
//    /// <response code="200">Returns summary of processed instances</response>
//    /// <response code="400">If the request is null or contains no instance IDs</response>
//    [HttpPost("instances")]
//    [ProducesResponseType(typeof(MultiInstanceCheckResult), 200)]
//    [ProducesResponseType(400)]
//    [SwaggerOperation(
//        Summary = "Check and send for all use cases from multiple instances",
//        Description = "Checks all use cases for completeness for multiple instances and sends Kafka messages for complete ones. Automatically handles duplicate subgraphs.",
//      Tags = new[] { "Completeness" }
//    )]
//    public async Task<IActionResult> CheckAndSendFromMultipleInstances([FromBody] MultiInstanceCheckRequest request)
//    {
//        if (request == null || request.InstanceIds == null || !request.InstanceIds.Any())
//        {
//            return BadRequest("At least one instance ID must be provided");
//        }

//        try
//        {
//            var startTime = DateTime.UtcNow;
//            _logger.LogInformation("Checking all use cases from {Count} instances", request.InstanceIds.Length);

//            await _completenessCheck.CheckAndSendAsync(request.InstanceIds);

//            var endTime = DateTime.UtcNow;
//            var duration = endTime - startTime;

//            var result = new MultiInstanceCheckResult
//            {
//                InstanceCount = request.InstanceIds.Length,
//                ValidInstanceCount = request.InstanceIds.Count(id => !string.IsNullOrEmpty(id)),
//                ProcessedAt = endTime,
//                DurationSeconds = duration.TotalSeconds
//            };

//            _logger.LogInformation("Completed checking {Count} instances in {Duration} seconds",
//         request.InstanceIds.Length, duration.TotalSeconds);

//            return Ok(result);
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Error checking use cases from multiple instances");
//            return StatusCode(500, $"Error checking use cases: {ex.Message}");
//        }
//    }
//}

///// <summary>
///// Result of a completeness check for a single use case from a specific instance
///// </summary>
//public class CompletenessCheckResult
//{
//    /// <summary>
//    /// The ID of the use case that was checked
//    /// </summary>
//    public string UseCaseId { get; set; } = string.Empty;

//    /// <summary>
//    /// The ID of the instance used as starting point
//    /// </summary>
//    public string StartInstanceId { get; set; } = string.Empty;

//    /// <summary>
//    /// Whether the graph is complete for this use case
//    /// </summary>
//    public bool IsComplete { get; set; }

//    /// <summary>
//    /// When the check was performed
//    /// </summary>
//    public DateTime CheckedAt { get; set; }
//}

///// <summary>
///// Result of finding complete subgraphs for a use case
///// </summary>
//public class CompleteSubgraphsResult
//{
//    /// <summary>
//    /// The ID of the use case that was checked
//    /// </summary>
//    public string UseCaseId { get; set; } = string.Empty;

//    /// <summary>
//    /// Number of complete subgraphs found
//    /// </summary>
//    public int CompleteSubgraphCount { get; set; }

//    /// <summary>
//    /// List of root instance IDs for each complete subgraph
//    /// </summary>
//    public List<string> RootInstanceIds { get; set; } = new();

//    /// <summary>
//    /// When the check was performed
//    /// </summary>
//    public DateTime CheckedAt { get; set; }
//}

///// <summary>
///// Result of a global completeness check across all use cases
///// </summary>
//public class GlobalCompletenessResult
//{
//    /// <summary>
//    /// When the check started
//    /// </summary>
//    public DateTime CheckStartedAt { get; set; }

//    /// <summary>
//    /// When the check completed
//    /// </summary>
//    public DateTime CheckCompletedAt { get; set; }

//    /// <summary>
//    /// Duration of the check in seconds
//    /// </summary>
//    public double DurationSeconds { get; set; }

//    /// <summary>
//    /// Total number of complete subgraphs found
//    /// </summary>
//    public int TotalSubgraphs { get; set; }

//    /// <summary>
//    /// Result message
//    /// </summary>
//    public string Message { get; set; } = string.Empty;
//}

///// <summary>
///// Status information for the completeness check service
///// </summary>
//public class CompletenessStatusResult
//{
//    /// <summary>
//    /// Name of the service
//    /// </summary>
//    public string ServiceName { get; set; } = string.Empty;

//    /// <summary>
//    /// Current status
//    /// </summary>
//    public string Status { get; set; } = string.Empty;

//    /// <summary>
//    /// Service version
//    /// </summary>
//    public string Version { get; set; } = string.Empty;

//    /// <summary>
//    /// Current timestamp
//    /// </summary>
//    public DateTime Timestamp { get; set; }
//}

///// <summary>
///// Request for checking multiple instances
///// </summary>
//public class MultiInstanceCheckRequest
//{
//    /// <summary>
//    /// Array of instance IDs to check for completeness
//    /// </summary>
//    public string[] InstanceIds { get; set; } = Array.Empty<string>();
//}

///// <summary>
///// Result of checking multiple instances
///// </summary>
//public class MultiInstanceCheckResult
//{
//    /// <summary>
//    /// Total number of instance IDs provided in the request
//    /// </summary>
//    public int InstanceCount { get; set; }

//    /// <summary>
//    /// Number of valid (non-null, non-empty) instance IDs that were processed
//    /// </summary>
//    public int ValidInstanceCount { get; set; }

//    /// <summary>
//    /// When the processing was completed
//    /// </summary>
//    public DateTime ProcessedAt { get; set; }

//    /// <summary>
//    /// Duration of the check in seconds
//    /// </summary>
//    public double DurationSeconds { get; set; }
//}
