// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Ekkodale.TelemetryExtensions;
using InstanceService.Api.Extensions.ServiceExtensions;
using InstanceService.Api.Hubs;
using InstanceService.Api.MapperProfiles;
using InstanceService.Api.Messaging.Consumers;
using InstanceService.Api.Messaging.Consumers.Guidelines;
using InstanceService.Api.Messaging.Consumers.Ontology;
using InstanceService.Api.Options;
using InstanceService.Api.Services;
using InstanceService.Api.Utilities;
using InstanceService.Api.Utilities.Interfaces;
using InstanceService.Api.Utilities.Provider;
using InstanceService.Data;
using InstanceService.Data.Options;
using InstanceService.Data.Repositories;
using InstanceService.Domain.IRepositories;
using MassTransit;
using Messaging.Core.Extensions.Mediator;
using Microsoft.AspNetCore.HttpOverrides;
using Minio;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using InstanceService.Models;
using System.Reflection;
using System.Text.Json.Serialization;
using Throw;

var builder = WebApplication.CreateBuilder(args);
ConfigurationManager configuration = builder.Configuration;

TelemetryOptions? telOpts = configuration.GetSection("OpenTelemetry").Get<TelemetryOptions>();
telOpts.ThrowIfNull("OpenTelemetry configuration is missing");
builder.AddMonitoring(telOpts, Assembly.GetExecutingAssembly());

// Add options and other services
builder.Services.AddOptions<PostgresOptions>()
    .Bind(builder.Configuration.GetSection(PostgresOptions.Postgres))
    .ValidateDataAnnotations();
builder.Services.AddOptions<GremlinOptions>()
    .Bind(builder.Configuration.GetSection(GremlinOptions.SectionName))
    .ValidateDataAnnotations();
builder.Services.AddOptions<KafkaOptions>()
    .Bind(builder.Configuration.GetSection(KafkaOptions.Kafka))
    .ValidateDataAnnotations();
builder.Services.AddOptions<KeycloakOptions>()
    .Bind(builder.Configuration.GetSection(KeycloakOptions.Keycloak))
    .ValidateDataAnnotations();
builder.Services.AddOptions<AccessOptions>()
    .Bind(builder.Configuration.GetSection(AccessOptions.Access))
    .ValidateDataAnnotations();
builder.Services.AddOptions<UsecaseOptions>()
    .Bind(builder.Configuration.GetSection(UsecaseOptions.UseCase))
    .ValidateDataAnnotations();

// Register AutoMapper with DI
builder.Services.AddAutoMapper(cfg =>
{
    cfg.AddProfile<InstanceProfile>();
    cfg.AddProfile<InstanceMetadataProfile>();
    cfg.AddProfile<InstanceRelationProfile>();
}, typeof(Program));

// Register other services
builder.Services.AddTransient<IGuidelineProvider, GuidelineProvider>();
builder.Services.AddTransient<IOntologyProvider, OntologyDbProvider>();
builder.Services.AddTransient<IOntologyParserService, OntologyParserService>();
builder.Services.AddScoped<IGraphDataModelValidationService, GraphDataModelValidationService>();

// Guideline relational projection (event-driven ingestion from the GuidelineService topic)
builder.Services.AddScoped<IGuidelineProjectionRepository, GuidelineProjectionRepository>();
builder.Services.AddScoped<IGuidelineTransformationService, GuidelineTransformationService>();
builder.Services.AddScoped<IGuidelineReconstructionService, GuidelineReconstructionService>();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IInstanceRepository, GremlinInstanceRepository>();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Add Swagger services using the extension method
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger(configuration);
builder.Services.AddSwaggerGen(options =>
{
    options.CustomSchemaIds(type => type.ToString());
});

builder.Services.AddDataAccess();

#region CORS

// Conditionally configure CORS based on the environment
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder
            .AllowAnyOrigin()  // Allowing any origin
            .AllowAnyMethod()   // Allowing any HTTP method
            .AllowAnyHeader()); // Allowing any header
});

#endregion CORS

builder.Services.AddMemoryCache();
builder.Services.Configure<MinioOptions>(configuration.GetSection("Minio"));

// Shared MinIO client used by the guideline transformation service to stream large guideline files to a temp file.
builder.Services.AddScoped<IMinioClient>(_ =>
{
    var minioOptions = configuration.GetSection("Minio").Get<MinioOptions>()!;
    return (IMinioClient)new MinioClient()
        .WithEndpoint(minioOptions.Address)
        .WithCredentials(minioOptions.AccessKey, minioOptions.SecretKey)
        .WithSSL(minioOptions.Address.StartsWith("https", StringComparison.OrdinalIgnoreCase))
        .Build();
});
builder.Services.Configure<KafkaOptions>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddHttpClient<AccessRightsFetcher>();
builder.Services.AddTransient<IAccessRightsFetcher, AccessRightsFetcher>();
builder.Services.AddHttpClient<UseCaseFetcher>();
builder.Services.AddTransient<IUseCaseFetcher, UseCaseFetcher>();
builder.Services.AddHttpClient<UserGroupProvider>();
builder.Services.AddSingleton<IUserGroupProvider, UserGroupProvider>();
builder.Services.AddScoped<IAccessRightHelper, AccessRightHelper>();
builder.Services.AddTransient<ICypherToLinqTranslator, CypherToLinqTranslator>();
builder.Services.AddScoped<IMinioHelper, MinioHelper>();
builder.Services.AddTransient<IDynamicKafkaProducer, DynamicKafkaProducer>();
builder.Services.AddScoped<IGraphQueryExecutor, GremlinGraphQueryExecutor>();
builder.Services.AddScoped<ICompletenessCheck, CompletenessCheck>();

// Completeness checks traverse the whole subgraph, writers only schedule them.
builder.Services.AddSingleton<CompletenessCheckScheduler>();
builder.Services.AddSingleton<ICompletenessCheckScheduler>(sp => sp.GetRequiredService<CompletenessCheckScheduler>());
builder.Services.AddHostedService<CompletenessCheckWorker>();

// Graph change notifications. Writers only queue a use case ID, the worker coalesces the
// queue and tells the clients of that use case to refetch.
builder.Services.AddSignalR();
builder.Services.AddSingleton<GraphChangeNotifier>();
builder.Services.AddSingleton<IGraphChangeNotifier>(sp => sp.GetRequiredService<GraphChangeNotifier>());
builder.Services.AddHostedService<GraphChangeBroadcastWorker>();

#region Mediator (keeping internal messaging)

// Add MassTransit for internal mediator only (no Kafka)
Assembly assembly = Assembly.GetExecutingAssembly();
builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.SetInMemorySagaRepositoryProvider();
    x.AddConsumers(assembly);
    x.AddSagaStateMachines(assembly);
    x.AddSagas(assembly);
    x.AddActivities(assembly);
    x.AddMediator(delegate (IMediatorRegistrationConfigurator cfg)
    {
        cfg.RegisterConsumersFromAssembly(assembly);
    });
    x.UsingInMemory(); // Only in-memory for internal messaging
});

#endregion Mediator

#region Kafka

// Add Confluent.Kafka background service
builder.Services.AddHostedService<GraphDataModelConsumer>();
builder.Services.AddHostedService<OntologyConsumer>();
builder.Services.AddHostedService<GuidelineConsumer>();

#endregion Kafka

#region Authentication
builder.Services.AddKeycloakAuthentication(options =>
{
    configuration.GetSection("Keycloak").Bind(options);
});

#endregion

var app = builder.Build();

// Use CORS if configured
app.UseCors("AllowAllOrigins");

// Configure the HTTP request pipeline.
// Respect reverse proxy headers (Traefik) for scheme/host
var fwdOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedHost | ForwardedHeaders.XForwardedProto
};
fwdOptions.KnownNetworks.Clear();
fwdOptions.KnownProxies.Clear();
app.UseForwardedHeaders(fwdOptions);

app.UseSwagger(c =>
{
    c.PreSerializeFilters.Add((swagger, httpReq) =>
    {
        var scheme = httpReq.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? httpReq.Scheme;
        var host = httpReq.Headers["X-Forwarded-Host"].FirstOrDefault() ?? httpReq.Host.Value;
        var basePath = httpReq.Headers["X-Forwarded-Prefix"].FirstOrDefault() ?? httpReq.PathBase.Value ?? string.Empty;

        swagger.Servers = [
            new OpenApiServer { Url = $"{scheme}://{host}{basePath}" }
        ];
    });
});

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("v1/swagger.json", "v1");
    options.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// Unauthenticated by design, see GraphHub: the notification is a use case ID and nothing else.
app.MapHub<GraphHub>("/hubs/graph");

app.AddSwagger();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InstanceServiceDbContext>();
    await db.Database.MigrateAsync();
}

await app.RunAsync();