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
using Minio;
using Minio.DataModel.Args;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InstanceService.Api.Messaging.Consumers.Ontology;

// Field names must match the JSON produced by OntologyService's events
// (serialized with default System.Text.Json options, i.e. PascalCase).

/// <summary>Event published by OntologyService when an ontology file is uploaded or replaced.</summary>
internal sealed record OntologyUploadedEvent(
    [property: JsonPropertyName("Id")] string Id,
    [property: JsonPropertyName("Etag")] string Etag,
    [property: JsonPropertyName("Bucket")] string Bucket,
    [property: JsonPropertyName("ObjectKey")] string ObjectKey,
    [property: JsonPropertyName("ContentType")] string ContentType);

/// <summary>Event published by OntologyService when an ontology is deleted.</summary>
internal sealed record OntologyDeletedEvent(
    [property: JsonPropertyName("Id")] string Id,
    [property: JsonPropertyName("Bucket")] string Bucket,
    [property: JsonPropertyName("ObjectKey")] string ObjectKey);

/// <summary>
/// Background service that consumes ontology events from Kafka and routes on the <c>event_type</c> header:
/// <c>UploadedOntologyFile</c> downloads the TTL file and re-parses it into the relational ontology tables
/// (replacing the previous version); <c>DeletedOntology</c> removes the stored ontology projection.
/// </summary>
public class OntologyConsumer(
    ILogger<OntologyConsumer> logger,
    IServiceProvider serviceProvider,
    IOptions<KafkaOptions> kafkaOptions)
    : KafkaConsumerBase(logger, serviceProvider, kafkaOptions, "Ontology")
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ProcessMessage(
        string messageValue, IReadOnlyDictionary<string, string> headers, CancellationToken stoppingToken)
    {
        headers.TryGetValue("event_type", out var eventType);

        switch (eventType)
        {
            case "UploadedOntologyFile":
                await HandleUploadAsync(messageValue, stoppingToken);
                break;

            case "DeletedOntology":
                await HandleDeleteAsync(messageValue, stoppingToken);
                break;

            default:
                _logger.LogWarning(
                    "Unknown or missing event_type header '{EventType}' on Ontology topic. Message skipped.",
                    eventType);
                break;
        }
    }

    private async Task HandleUploadAsync(string messageValue, CancellationToken stoppingToken)
    {
        OntologyUploadedEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<OntologyUploadedEvent>(messageValue, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize OntologyUploadedEvent message");
            return;
        }

        if (evt is null)
        {
            _logger.LogWarning("Failed to deserialize message to OntologyUploadedEvent");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var minioClient = scope.ServiceProvider.GetRequiredService<IMinioClient>();
        var parserService = scope.ServiceProvider.GetRequiredService<IOntologyParserService>();

        var ttlBytes = await DownloadAsync(minioClient, evt.Bucket, evt.ObjectKey, stoppingToken);

        await parserService.ParseAndStoreAsync(ttlBytes, evt.Id, evt.Etag, stoppingToken);

        _logger.LogInformation(
            "Processed UploadedOntologyFile event for OntologyId {OntologyId}, ObjectKey {ObjectKey}, Etag {Etag}",
            evt.Id, evt.ObjectKey, evt.Etag);
    }

    private async Task HandleDeleteAsync(string messageValue, CancellationToken stoppingToken)
    {
        OntologyDeletedEvent? evt;
        try
        {
            evt = JsonSerializer.Deserialize<OntologyDeletedEvent>(messageValue, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize OntologyDeletedEvent message");
            return;
        }

        if (evt is null)
        {
            _logger.LogWarning("Failed to deserialize message to OntologyDeletedEvent");
            return;
        }

        using var scope = _serviceProvider.CreateScope();
        var parserService = scope.ServiceProvider.GetRequiredService<IOntologyParserService>();

        await parserService.DeleteByOntologyIdAsync(evt.Id, stoppingToken);

        _logger.LogInformation("Processed DeletedOntology event for OntologyId {OntologyId}", evt.Id);
    }

    /// <summary>Downloads an object from MinIO into memory (TTL/RDF files are small enough to hold in memory).</summary>
    private static async Task<byte[]> DownloadAsync(IMinioClient minioClient, string bucket, string objectKey, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await minioClient.GetObjectAsync(new GetObjectArgs()
            .WithBucket(bucket)
            .WithObject(objectKey)
            .WithCallbackStream(stream => stream.CopyTo(ms)), ct);
        return ms.ToArray();
    }
}
