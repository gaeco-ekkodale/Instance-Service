// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Bogus;
using FluentAssertions;
using Guideline.Model.Model;
using InstanceService.Api.Messaging.Consumers.Internal;
using InstanceService.Api.Messaging.Consumers.Guidelines.Contracts;
using InstanceService.Api.Messaging.Consumers.Internal.Contracts;
using InstanceService.Api.Tests.Utilities.Faker;
using InstanceService.Api.Utilities;
using InstanceService.Api.Utilities.Provider;
using InstanceService.Domain.IRepositories;
using Messaging.Core.Abstractions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using InstanceService.Models;
using InstanceService.Models.Enum;

namespace InstanceService.Api.Tests.Messaging.Consumers.Internal;

public class UseCaseDataUpdatedConsumerTests
{
    private readonly ILogger<IInternalRequestConsumer<UseCaseGuidelineDataUpdated, UseCaseGuidelineDataUpdatedResponse>> _logger;
    private readonly ILogger<IInternalRequestConsumer<CreateReducedGuideline, CreateReducedGuidelineResponse>> _guidelineLogger;
    private readonly IAccessRightsFetcher _accessRightsFetcher;
    private readonly IUseCaseFetcher _usecaseFetcher;
    private readonly IDynamicKafkaProducer _dynamicKafkaProducer;
    private readonly IInstanceRepository _instanceRepository;
    private readonly IGuidelineProvider _guidelineProvider;
    private readonly UseCaseDataUpdatedConsumer _consumer;
    private readonly Faker _faker;
    private readonly AccessRightFaker _accessRightFaker;

    public UseCaseDataUpdatedConsumerTests()
    {
        _logger = Substitute.For<ILogger<IInternalRequestConsumer<UseCaseGuidelineDataUpdated, UseCaseGuidelineDataUpdatedResponse>>>();
        _guidelineLogger = Substitute.For<ILogger<IInternalRequestConsumer<CreateReducedGuideline, CreateReducedGuidelineResponse>>>();
        _accessRightsFetcher = Substitute.For<IAccessRightsFetcher>();
        _usecaseFetcher = Substitute.For<IUseCaseFetcher>();
        _dynamicKafkaProducer = Substitute.For<IDynamicKafkaProducer>();
        _instanceRepository = Substitute.For<IInstanceRepository>();
        _guidelineProvider = Substitute.For<IGuidelineProvider>();

        _consumer = new UseCaseDataUpdatedConsumer(
            _logger,
            _guidelineLogger,
            _accessRightsFetcher,
            _dynamicKafkaProducer,
            _instanceRepository,
            _usecaseFetcher,
            _guidelineProvider);

        _faker = new Faker();
        _accessRightFaker = new AccessRightFaker();
    }
    [Fact]
    public async Task When_GraphDataModelIsExported_Then_GraphDataModelContainsAccessAndUsecaseObjects()
    {
        // Arrange
        var useCaseId = _faker.Random.Guid().ToString();
        var classificationId = _faker.Random.Guid().ToString();
        var request = new UseCaseGuidelineDataUpdated { UseCaseId = useCaseId };

        var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUseCaseId(useCaseId)
                .WithUserGroupId(_faker.Random.Guid().ToString())
                .WithRight(PropertyRight.Read)
                .Generate()
        };

        var instances = new List<Instance>
        {
            new()
            {
                Id = _faker.Random.Guid().ToString(),
                ClassificationId = classificationId,
                Properties = new Dictionary<string, string>
                {
                    { "property1", _faker.Lorem.Word() },
                    { "property2", _faker.Lorem.Word() }
                },
                Relations = new List<InstanceRelation>()
            }
        };
        var description = _faker.Lorem.Words();

        var usecase = new UseCase
        {
            Id = useCaseId,
            Description = string.Join(" ", description),
            Name = _faker.Lorem.Word()
        };

        _accessRightsFetcher.GetAccessRightsAsync().Returns(accessRights);
        _instanceRepository.GetInstances(withMetadata: true).Returns(instances);
        _usecaseFetcher.GetUseCasesByIdAsync(useCaseId).Returns(usecase);

        // Act
        var result = await _consumer.ConsumeInternal(request);

        // Assert
        await _dynamicKafkaProducer.Received(1).ProduceToDynamicTopicAsync(
        Arg.Is<GraphDataModel>(gdm =>
            gdm.UseCase != null &&
            gdm.UseCase.Id == useCaseId &&
            gdm.AccessRights is List<AccessRight> &&
            gdm.AccessRights.Any() &&
            gdm.AccessRights.All(r => r.UseCaseId.ToString() == useCaseId)
        ),
        Arg.Any<string>(),
        Arg.Any<Dictionary<string, object>>());

        result.GraphDataModels.Should().AllSatisfy(gdm =>
        {
            gdm.UseCase.Should().NotBeNull("because every GDM must have a UseCase");
            gdm.UseCase.Id.Should().Be(useCaseId, "because the UseCase ID must match the requested ID");

            gdm.AccessRights.Should().NotBeNull("because the AccessRights list should not be null");
            gdm.AccessRights.Should().BeOfType<List<AccessRight>>("because the type must be List<AccessRight>");
            gdm.AccessRights.Should().NotBeEmpty("because the AccessRights list must contain at least one element");
            gdm.AccessRights.Should().OnlyContain(r => r.UseCaseId.ToString() == useCaseId,
                "because all AccessRights must relate to the same UseCase");
        });
    }
    [Fact]
    public async Task When_ValidUseCaseDataUpdatedWithAccessibleInstances_Then_ProducesGraphDataModelToKafka()
    {
        // Arrange
        var useCaseId = _faker.Random.Guid().ToString();
        var classificationId = _faker.Random.Guid().ToString();
        var request = new UseCaseGuidelineDataUpdated { UseCaseId = useCaseId };

        var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUseCaseId(useCaseId)
                .WithUserGroupId(_faker.Random.Guid().ToString())
                .WithRight(PropertyRight.Read)
                .Generate()
        };

        var instances = new List<Instance>
        {
            new()
            {
                Id = _faker.Random.Guid().ToString(),
                ClassificationId = classificationId,
                Properties = new Dictionary<string, string>
                {
                    { "property1", _faker.Lorem.Word() },
                    { "property2", _faker.Lorem.Word() }
                },
                Relations = new List<InstanceRelation>()
            }
        };

        _accessRightsFetcher.GetAccessRightsAsync().Returns(accessRights);
        _instanceRepository.GetInstances(withMetadata: true).Returns(instances);
        _usecaseFetcher.GetUseCasesByIdAsync(useCaseId).Returns(new UseCase { Id = useCaseId, Name = _faker.Lorem.Word(), Description = _faker.Lorem.Sentence() });

        // Act
        await _consumer.ConsumeInternal(request);

        // Assert
        await _dynamicKafkaProducer.Received(1).ProduceToDynamicTopicAsync(
            Arg.Any<GraphDataModel>(),
            $"ekkodale.gaeco.instance.public.{useCaseId}.gaecoExport",
            Arg.Is<Dictionary<string, object>>(headers =>
                headers["useCase"].ToString() == useCaseId &&
                headers["usergroup"].ToString() == "gaeco" &&
                headers["version"].ToString() == "v1" &&
                headers["event"].ToString() == "dump" &&
                headers["entity"].ToString() == "GraphDataModel"));
    }

    [Fact]
    public async Task When_NoAccessibleInstancesForUseCase_Then_NoGraphDataModelProduced()
    {
        // Arrange
        var useCaseId = _faker.Random.Guid().ToString();
        var differentUseCaseId = _faker.Random.Guid().ToString();
        var classificationId = _faker.Random.Guid().ToString();
        var request = new UseCaseGuidelineDataUpdated { UseCaseId = useCaseId };

        var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUseCaseId(differentUseCaseId) // Different use case ID
                .WithUserGroupId(_faker.Random.Guid().ToString())
                .WithRight(PropertyRight.Read)
                .Generate()
        };

        var instances = new List<Instance>
        {
            new()
            {
                Id = _faker.Random.Guid().ToString(),
                ClassificationId = classificationId,
                Properties = new Dictionary<string, string>(),
                Relations = new List<InstanceRelation>()
            }
        };

        _accessRightsFetcher.GetAccessRightsAsync().Returns(accessRights);
        _instanceRepository.GetInstances(withMetadata: true).Returns(instances);
        _usecaseFetcher.GetUseCasesByIdAsync(useCaseId).Returns(new UseCase { Id = useCaseId, Name = _faker.Lorem.Word(), Description = _faker.Lorem.Sentence() });

        // Act
        await _consumer.ConsumeInternal(request);

        // Assert
        await _dynamicKafkaProducer.DidNotReceive().ProduceToDynamicTopicAsync(
            Arg.Any<GraphDataModel>(),
            Arg.Any<string>(),
            Arg.Any<Dictionary<string, object>>());
    }

    [Fact]
    public async Task When_MultipleConnectedInstancesExist_Then_ProducesSingleGraphDataModelForConnectedComponent()
    {
        // Arrange
        var useCaseId = _faker.Random.Guid().ToString();
        var classificationId = _faker.Random.Guid().ToString();
        var request = new UseCaseGuidelineDataUpdated { UseCaseId = useCaseId };

        var instance1Id = _faker.Random.Guid().ToString();
        var instance2Id = _faker.Random.Guid().ToString();
        var relationUri = $"https://ibpdi.org/ontology/2.0/{_faker.Lorem.Word()}";

        var accessRights = new List<AccessRight>
        {
            _accessRightFaker
                .WithClassificationId(classificationId)
                .WithUseCaseId(useCaseId)
                .WithUserGroupId(_faker.Random.Guid().ToString())
                .WithRight(PropertyRight.Write)
                .Generate()
        };

        var instances = new List<Instance>
        {
            new()
            {
                Id = instance1Id,
                ClassificationId = classificationId,
                Properties = new Dictionary<string, string>
                {
                    { "name", _faker.Person.FirstName }
                },
                Relations = new List<InstanceRelation>
                {
                    new()
                    {
                        SubjectId = instance1Id,
                        ObjectId = instance2Id,
                        PredicateUri = relationUri
                    }
                }
            },
            new()
            {
                Id = instance2Id,
                ClassificationId = classificationId,
                Properties = new Dictionary<string, string>
                {
                    { "description", _faker.Lorem.Sentence() }
                },
                Relations = new List<InstanceRelation>
                {
                    new()
                    {
                        SubjectId = instance1Id,
                        ObjectId = instance2Id,
                        PredicateUri = relationUri
                    }
                }
            }
        };

        _accessRightsFetcher.GetAccessRightsAsync().Returns(accessRights);
        _instanceRepository.GetInstances(withMetadata: true).Returns(instances);
        _usecaseFetcher.GetUseCasesByIdAsync(useCaseId).Returns(new UseCase { Id = useCaseId, Name = _faker.Lorem.Word(), Description = _faker.Lorem.Sentence() });

        // Act
        await _consumer.ConsumeInternal(request);

        // Assert
        await _dynamicKafkaProducer.Received(1).ProduceToDynamicTopicAsync(
            Arg.Is<GraphDataModel>(model =>
                model.GraphMetadata != null &&
                model.GraphMetadata.Count == 2 &&
                model.GraphMetadata.Any(node => node.Id == instance1Id) &&
                model.GraphMetadata.Any(node => node.Id == instance2Id)),
            $"ekkodale.gaeco.instance.public.{useCaseId}.gaecoExport",
            Arg.Any<Dictionary<string, object>>());
    }
}