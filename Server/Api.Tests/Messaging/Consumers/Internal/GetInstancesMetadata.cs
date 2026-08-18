// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Guideline.Model.Model;
using InstanceService.Api.Messaging.Consumers.Internal;
using InstanceService.Api.Messaging.Consumers.Internal.Contracts;
using InstanceService.Api.Services;
using InstanceService.Api.Utilities;
using InstanceService.Api.Utilities.Provider;
using InstanceService.Data;
using InstanceService.Models;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using InstanceService.Models.Enum;

namespace InstanceService.Api.Tests.Messaging.Consumers.Internal;

public class GetInstancesMetadataConsumerTests : IAsyncLifetime
{
    private ServiceProvider _provider;
    private ITestHarness _harness;
    private IConsumerTestHarness<GetInstancesMetadataRequestConsumer> _consumerHarness;

    public async Task InitializeAsync()
    {
        var mockedAccessRightsFetcher = Substitute.For<IAccessRightsFetcher>();
        var mockedReconstructionService = Substitute.For<IGuidelineReconstructionService>();
        var mockedUserGroupProvider = Substitute.For<IUserGroupProvider>();
        var mockedAccessRightHelper = Substitute.For<IAccessRightHelper>();

        _provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<GetInstancesMetadataRequestConsumer>();
                cfg.UsingInMemory((context, config) =>
                {
                    config.ConfigureEndpoints(context);
                });
            })
            .AddDbContext<InstanceServiceDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName: "TestInstancesMetadataDb");
            })
            .AddSingleton(mockedAccessRightsFetcher)
            .AddSingleton(mockedReconstructionService)
            .AddSingleton(mockedUserGroupProvider)
            .AddSingleton(mockedAccessRightHelper)
            .AddLogging()
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        _consumerHarness = _provider.GetRequiredService<IConsumerTestHarness<GetInstancesMetadataRequestConsumer>>();
        await _harness.Start();
    }

    public async Task DisposeAsync()
    {
        await _harness.Stop();
        await _provider.DisposeAsync();
    }

    private async Task ResetDatabaseAsync()
    {
        using var scope = _provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InstanceServiceDbContext>();
        dbContext.InstanceMetadata.RemoveRange(dbContext.InstanceMetadata);
        await dbContext.SaveChangesAsync();
    }

    private void PrepareDefault()
    {
        var accessRightHelperSubstitute = _provider.GetRequiredService<IAccessRightHelper>();
        _provider.GetRequiredService<IGuidelineReconstructionService>().GetClassificationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((IClassification?)null);
        _provider.GetRequiredService<IAccessRightsFetcher>().GetAccessRightsAsync().Returns(Task.FromResult<IEnumerable<AccessRight>>(new List<AccessRight>()));
        _provider.GetRequiredService<IUserGroupProvider>().GetUserGroupIdsAsync(Arg.Any<string>()).Returns(Task.FromResult(new List<string>()));
        accessRightHelperSubstitute.CanGetMetadata(Arg.Any<string>(), Arg.Any<List<string>>(), Arg.Any<IEnumerable<AccessRight>>(), Arg.Any<string>()).Returns(true);
        accessRightHelperSubstitute.GetFilteredAccessRights(Arg.Any<string>(), Arg.Any<List<string>>(), Arg.Any<IEnumerable<AccessRight>>(), Arg.Any<string>()).Returns(new List<AccessRight>());
    }

    [Fact]
    public async Task Should_Respond_With_Correct_Response_When_Request_Is_Valid()
    {
        // Arrange
        await ResetDatabaseAsync();

        var bus = _provider.GetRequiredService<IBus>();
        var requestClient = bus.CreateRequestClient<GetInstancesMetadataRequest>();

        PrepareDefault();
        var testInstanceIds = new List<string>();
        testInstanceIds.AddRange(["valid_instance_id_1", "valid_instance_id_2", "valid_instance_id_3"]);

        using var scope = _provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InstanceServiceDbContext>();
        testInstanceIds.ForEach(id => dbContext.InstanceMetadata.Add(new InstanceMetaData
        {
            Id = id,
            Properties = []
        }));
        await dbContext.SaveChangesAsync();

        // Act
        var response = await requestClient.GetResponse<GetInstancesMetadataResponse>(new GetInstancesMetadataRequest
        {
            InstanceIds = testInstanceIds,
            UseCaseId = "",
            Token = ""
        });

        // Assert
        testInstanceIds.ForEach(id =>
        {
            var resultInstance = response.Message.InstanceData.Where(instance => instance.Metadata.Id == id).SingleOrDefault();
            Assert.NotNull(resultInstance);
            Assert.Empty(resultInstance.MetadataProperties);
            Assert.Equal("", resultInstance.ClassificationName);
        });
    }

    [Fact]
    public async Task Should_Return_Empty_When_User_Is_Unauthenticated()
    {
        // Arrange
        await ResetDatabaseAsync();

        var bus = _provider.GetRequiredService<IBus>();
        var requestClient = bus.CreateRequestClient<GetInstancesMetadataRequest>();

        PrepareDefault();
        var testInstanceIds = new List<string>();
        testInstanceIds.AddRange(["valid_instance_id_1", "valid_instance_id_2", "valid_instance_id_3"]);

        var accessRightHelperSubstitute = _provider.GetRequiredService<IAccessRightHelper>();
        accessRightHelperSubstitute.CanGetMetadata(Arg.Any<string>(), Arg.Any<List<string>>(), Arg.Any<IEnumerable<AccessRight>>(), Arg.Any<string>()).Returns(false);

        using var scope = _provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InstanceServiceDbContext>();
        testInstanceIds.ForEach(id => dbContext.InstanceMetadata.Add(new InstanceMetaData
        {
            Id = id,
            Properties = []
        }));
        await dbContext.SaveChangesAsync();

        // Act
        var response = await requestClient.GetResponse<GetInstancesMetadataResponse>(new GetInstancesMetadataRequest
        {
            InstanceIds = testInstanceIds,
            UseCaseId = "",
            Token = ""
        });

        // Assert
        Assert.Empty(response.Message.InstanceData);
    }

    [Fact]
    public async Task Should_Return_Empty_When_Request_Is_Invalid()
    {
        // Arrange
        await ResetDatabaseAsync();

        var bus = _provider.GetRequiredService<IBus>();
        var requestClient = bus.CreateRequestClient<GetInstancesMetadataRequest>();

        var testInstanceIds = new List<string>();
        testInstanceIds.AddRange(["invalid_instance_id_1", "invalid_instance_id_2"]);

        // Act
        var response = await requestClient.GetResponse<GetInstancesMetadataResponse>(new GetInstancesMetadataRequest
        {
            InstanceIds = testInstanceIds,
            UseCaseId = "",
            Token = ""
        });

        // Assert
        Assert.Empty(response.Message.InstanceData);
    }

    [Fact]
    public async Task Should_Return_Partial_Result_When_Request_Is_Invalid()
    {
        // Arrange
        await ResetDatabaseAsync();

        var bus = _provider.GetRequiredService<IBus>();
        var requestClient = bus.CreateRequestClient<GetInstancesMetadataRequest>();

        PrepareDefault();
        var validInstanceId = "valid_instance_id_1";
        var invalidInstanceId = "invalid_instance_id_2";
        var testInstanceIds = new List<string>();
        testInstanceIds.AddRange([validInstanceId, invalidInstanceId]);

        using var scope = _provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InstanceServiceDbContext>();
        var metaDataToAdd = new InstanceMetaData
        {
            Id = validInstanceId,
            Properties = []
        };
        dbContext.InstanceMetadata.Add(metaDataToAdd);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await requestClient.GetResponse<GetInstancesMetadataResponse>(new GetInstancesMetadataRequest
        {
            InstanceIds = testInstanceIds,
            UseCaseId = "",
            Token = ""
        });

        // Assert
        Assert.Single(response.Message.InstanceData);
        Assert.NotNull(response.Message.InstanceData.Where(instance => instance.Metadata.Id == validInstanceId).SingleOrDefault());
    }

    [Fact]
    public async Task Should_Respond_With_Property_Readonly_True_When_Request_Is_Valid_And_Property_Read()
    {
        // Arrange
        await ResetDatabaseAsync();

        var bus = _provider.GetRequiredService<IBus>();
        var requestClient = bus.CreateRequestClient<GetInstancesMetadataRequest>();

        PrepareDefault();
        var testPropertyName = "TestProperty";
        var testPropertyValue = "TestPropertyValue";

        var testInstanceIds = new List<string>();
        testInstanceIds.AddRange(["invalid_instance_id_1", "invalid_instance_id_2"]);

        var mockProperty = Substitute.For<IProperty>();
        mockProperty.Name.Returns(testPropertyName);
        var mockPropertyAssignment = Substitute.For<IPropertyAssignment>();
        mockPropertyAssignment.Property.Returns(mockProperty);
        var mockClassificationProperty = Substitute.For<IClassificationProperty>();
        mockClassificationProperty.PropertyAssignment.Returns(mockPropertyAssignment);
        var mockClassification = Substitute.For<IClassification>();
        mockClassification.ClassificationProperties.Returns(new List<IClassificationProperty> { mockClassificationProperty });
        _provider.GetRequiredService<IGuidelineReconstructionService>()
            .GetClassificationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(mockClassification);

        var accessRightHelperSubstitute = _provider.GetRequiredService<IAccessRightHelper>();

        accessRightHelperSubstitute.FilterSingleAccessRight(
            Arg.Any<IClassificationProperty>(),
            Arg.Any<IEnumerable<AccessRight>>(),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<List<string>>()).Returns(
            new AccessRight
            {
                Name = testPropertyName,
                Right = PropertyRight.Read
            }
        );

        using var scope = _provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InstanceServiceDbContext>();

        var properties = new Dictionary<string, string>();
        properties.Add(testPropertyName, testPropertyValue);
        testInstanceIds.ForEach(id => dbContext.InstanceMetadata.Add(new InstanceMetaData
        {
            Id = id,
            Properties = properties
        }));
        await dbContext.SaveChangesAsync();

        // Act
        var response = await requestClient.GetResponse<GetInstancesMetadataResponse>(new GetInstancesMetadataRequest
        {
            InstanceIds = testInstanceIds,
            UseCaseId = "",
            Token = ""
        });

        // Assert
        foreach (var instance in response.Message.InstanceData)
        {
            var res = instance.MetadataProperties.Where(res => res.Name == testPropertyName).SingleOrDefault();
            Assert.NotNull(res);
            Assert.True(res.IsReadOnly);
            Assert.Equal(testPropertyValue, res.Value);
        }
    }

    [Fact]
    public async Task Should_Respond_With_Property_Readonly_False_When_Request_Is_Valid_And_Property_Write()
    {
        // Arrange
        await ResetDatabaseAsync();

        var bus = _provider.GetRequiredService<IBus>();
        var requestClient = bus.CreateRequestClient<GetInstancesMetadataRequest>();

        PrepareDefault();
        var testPropertyName = "TestProperty";
        var testPropertyValue = "TestPropertyValue";

        var testInstanceIds = new List<string>();
        testInstanceIds.AddRange(["invalid_instance_id_1", "invalid_instance_id_2"]);

        var mockProperty = Substitute.For<IProperty>();
        mockProperty.Name.Returns(testPropertyName);
        var mockPropertyAssignment = Substitute.For<IPropertyAssignment>();
        mockPropertyAssignment.Property.Returns(mockProperty);
        var mockClassificationProperty = Substitute.For<IClassificationProperty>();
        mockClassificationProperty.PropertyAssignment.Returns(mockPropertyAssignment);
        var mockClassification = Substitute.For<IClassification>();
        mockClassification.ClassificationProperties.Returns(new List<IClassificationProperty> { mockClassificationProperty });
        _provider.GetRequiredService<IGuidelineReconstructionService>()
            .GetClassificationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(mockClassification);

        var accessRightHelperSubstitute = _provider.GetRequiredService<IAccessRightHelper>();

        accessRightHelperSubstitute.FilterSingleAccessRight(
            Arg.Any<IClassificationProperty>(),
            Arg.Any<IEnumerable<AccessRight>>(),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<List<string>>()).Returns(
            new AccessRight
            {
                Name = testPropertyName,
                Right = PropertyRight.Write
            }
        );

        using var scope = _provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InstanceServiceDbContext>();

        var properties = new Dictionary<string, string>();
        properties.Add(testPropertyName, testPropertyValue);
        testInstanceIds.ForEach(id => dbContext.InstanceMetadata.Add(new InstanceMetaData
        {
            Id = id,
            Properties = properties
        }));
        await dbContext.SaveChangesAsync();

        // Act
        var response = await requestClient.GetResponse<GetInstancesMetadataResponse>(new GetInstancesMetadataRequest
        {
            InstanceIds = testInstanceIds,
            UseCaseId = "",
            Token = ""
        });

        // Assert
        foreach (var instance in response.Message.InstanceData)
        {
            var res = instance.MetadataProperties.Where(res => res.Name == testPropertyName).SingleOrDefault();
            Assert.NotNull(res);
            Assert.False(res.IsReadOnly);
            Assert.Equal(testPropertyValue, res.Value);
        }
    }
}