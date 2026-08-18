// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Api.Options;
using InstanceService.Api.Services;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace InstanceService.Api.Messaging.Consumers.Guidelines;

/// <summary>
/// Background service that consumes guideline events from the GuidelineService topic and routes them
/// based on the <c>event_type</c> header:
/// <c>UploadedGuideline</c> triggers a full relational transformation and upsert (plus deletion of
/// instances whose classifications were removed); <c>DeletedGuideline</c> removes the projection and
/// deletes all instances of its classifications.
/// </summary>
public class GuidelineConsumer(
    ILogger<GuidelineConsumer> logger,
    IServiceProvider serviceProvider,
    IOptions<KafkaOptions> kafkaOptions)
    : KafkaConsumerBase(logger, serviceProvider, kafkaOptions, "Guidelines")
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ProcessMessage(
        string messageValue, IReadOnlyDictionary<string, string> headers, CancellationToken stoppingToken)
    {
        headers.TryGetValue("event_type", out var eventType);

        using var scope = _serviceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGuidelineTransformationService>();

        switch (eventType)
        {
            case "UploadedGuideline":
                var uploaded = JsonSerializer.Deserialize<UploadedGuideline>(messageValue, JsonOptions)
                    ?? throw new InvalidOperationException("Failed to deserialize UploadedGuideline payload.");
                await service.ProcessAsync(uploaded, stoppingToken);
                _logger.LogInformation(
                    "Processed UploadedGuideline event. Name={Name}, Etag={Etag}, CorrelationId={CorrelationId}",
                    uploaded.Name, uploaded.Etag, uploaded.CorrelationId);
                break;

            case "DeletedGuideline":
                var deleted = JsonSerializer.Deserialize<DeletedGuideline>(messageValue, JsonOptions)
                    ?? throw new InvalidOperationException("Failed to deserialize DeletedGuideline payload.");
                await service.DeleteAsync(deleted, stoppingToken);
                _logger.LogInformation(
                    "Processed DeletedGuideline event. Id={Id}, ObjectKey={ObjectKey}",
                    deleted.Id, deleted.ObjectKey);
                break;

            default:
                _logger.LogWarning(
                    "Unknown or missing event_type header '{EventType}' on Guidelines topic. Message skipped.",
                    eventType);
                break;
        }
    }
}
