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

[CollectionDefinition("InstanceRequestConsumerTests", DisableParallelization = true)]
public class GetInstanceMetadataConsumerTests : IAsyncLifetime
{
    private ServiceProvider _provider;
    private ITestHarness _harness;
    private IConsumerTestHarness<GetInstanceMetadataRequestConsumer> _consumerHarness;

    public async Task InitializeAsync()
    {
        var mockedAccessRightsFetcher = Substitute.For<IAccessRightsFetcher>();
        var mockedReconstructionService = Substitute.For<IGuidelineReconstructionService>();
        var mockedUserGroupProvider = Substitute.For<IUserGroupProvider>();
        var mockedAccessRightHelper = Substitute.For<IAccessRightHelper>();

        _provider = new ServiceCollection()
            .AddMassTransitTestHarness(cfg =>
            {
                cfg.AddConsumer<GetInstanceMetadataRequestConsumer>();
                cfg.UsingInMemory((context, config) =>
                {
                    config.ConfigureEndpoints(context);
                });
            })
            .AddDbContext<InstanceServiceDbContext>(options =>
            {
                options.UseInMemoryDatabase(databaseName: "TestInstanceMetadataDb");
            })
            .AddSingleton(mockedAccessRightsFetcher)
            .AddSingleton(mockedReconstructionService)
            .AddSingleton(mockedUserGroupProvider)
            .AddSingleton(mockedAccessRightHelper)
            .AddLogging()
            .BuildServiceProvider(true);

        _harness = _provider.GetRequiredService<ITestHarness>();
        _consumerHarness = _provider.GetRequiredService<IConsumerTestHarness<GetInstanceMetadataRequestConsumer>>();
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
        var requestClient = bus.CreateRequestClient<GetInstanceMetadataRequest>();

        PrepareDefault();
        var testInstanceId = "valid_instance_id";

        using var scope = _provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InstanceServiceDbContext>();
        var metaDataToAdd = new InstanceMetaData
        {
            Id = testInstanceId,
            Properties = []
        };
        dbContext.InstanceMetadata.Add(metaDataToAdd);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await requestClient.GetResponse<GetInstanceMetadataResponse>(new GetInstanceMetadataRequest
        {
            InstanceId = testInstanceId,
            UseCaseId = "",
            Token = ""
        });

        // Assert
        Assert.Equal(testInstanceId, response.Message.Metadata.Id);
        Assert.Empty(response.Message.MetadataProperties);
        Assert.Equal("", response.Message.ClassificationName);
    }

    [Fact]
    public async Task Should_Throw_Error_When_Request_Is_Valid_But_User_Unauthenticated()
    {
        // Arrange
        await ResetDatabaseAsync();

        var bus = _provider.GetRequiredService<IBus>();
        var requestClient = bus.CreateRequestClient<GetInstanceMetadataRequest>();

        PrepareDefault();
        var accessRightHelperSubstitute = _provider.GetRequiredService<IAccessRightHelper>();
        accessRightHelperSubstitute.CanGetMetadata(Arg.Any<string>(), Arg.Any<List<string>>(), Arg.Any<IEnumerable<AccessRight>>(), Arg.Any<string>()).Returns(false);
        var testInstanceId = "valid_instance_id";

        using var scope = _provider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<InstanceServiceDbContext>();
        var metaDataToAdd = new InstanceMetaData
        {
            Id = testInstanceId,
            Properties = []
        };
        dbContext.InstanceMetadata.Add(metaDataToAdd);
        await dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<RequestFaultException>(() =>
            requestClient.GetResponse<GetInstanceMetadataResponse>(new GetInstanceMetadataRequest
            {
                InstanceId = testInstanceId,
                UseCaseId = "",
                Token = ""
            })
        );
    }

    [Fact]
    public async Task Should_Throw_Error_When_Request_Is_Invalid()
    {
        // Arrange
        await ResetDatabaseAsync();

        var bus = _provider.GetRequiredService<IBus>();
        var requestClient = bus.CreateRequestClient<GetInstanceMetadataRequest>();

        PrepareDefault();
        var testInstanceId = "invalid_instance_id";

        // Act & Assert
        await Assert.ThrowsAsync<RequestFaultException>(() =>
            requestClient.GetResponse<GetInstanceMetadataResponse>(new GetInstanceMetadataRequest
            {
                InstanceId = testInstanceId,
                UseCaseId = "",
                Token = ""
            })
        );
    }

    [Fact]
    public async Task Should_Respond_With_Property_Readonly_True_When_Request_Is_Valid_And_Property_Read()
    {
        // Arrange
        await ResetDatabaseAsync();

        var bus = _provider.GetRequiredService<IBus>();
        var requestClient = bus.CreateRequestClient<GetInstanceMetadataRequest>();

        PrepareDefault();
        var testInstanceId = "valid_instance_id";
        var testPropertyName = "TestProperty";
        var testPropertyValue = "TestPropertyValue";

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
        var metaDataToAdd = new InstanceMetaData
        {
            Id = testInstanceId,
            Properties = properties
        };
        dbContext.InstanceMetadata.Add(metaDataToAdd);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await requestClient.GetResponse<GetInstanceMetadataResponse>(new GetInstanceMetadataRequest
        {
            InstanceId = testInstanceId,
            UseCaseId = "",
            Token = ""
        });

        // Assert
        var res = response.Message.MetadataProperties.Where(res => res.Name == testPropertyName).SingleOrDefault();
        Assert.NotNull(res);
        Assert.True(res.IsReadOnly);
        Assert.Equal(testPropertyValue, res.Value);
    }

    [Fact]
    public async Task Should_Respond_With_Property_Readonly_False_When_Request_Is_Valid_And_Property_Write()
    {
        // Arrange
        await ResetDatabaseAsync();

        var bus = _provider.GetRequiredService<IBus>();
        var requestClient = bus.CreateRequestClient<GetInstanceMetadataRequest>();

        PrepareDefault();
        var testInstanceId = "valid_instance_id";
        var testPropertyName = "TestProperty";
        var testPropertyValue = "TestPropertyValue";

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
        var metaDataToAdd = new InstanceMetaData
        {
            Id = testInstanceId,
            Properties = properties
        };
        dbContext.InstanceMetadata.Add(metaDataToAdd);
        await dbContext.SaveChangesAsync();

        // Act
        var response = await requestClient.GetResponse<GetInstanceMetadataResponse>(new GetInstanceMetadataRequest
        {
            InstanceId = testInstanceId,
            UseCaseId = "",
            Token = ""
        });

        // Assert
        var res = response.Message.MetadataProperties.Where(res => res.Name == testPropertyName).SingleOrDefault();
        Assert.NotNull(res);
        Assert.False(res.IsReadOnly);
        Assert.Equal(testPropertyValue, res.Value);
    }
}