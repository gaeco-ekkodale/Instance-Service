// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

// COMMENTED OUT — the reduced-guideline creation algorithm is to be extracted into a separate service.
// The HTTP endpoint (GuidelinesController) has been removed; this orchestration is preserved here for
// reference until it is moved. The reduction logic itself lives in GuidelineHelper.GetReducedGuideline.
//
// using GuidelineModelIO;
// using InstanceService.Api.Messaging.Consumers.Guidelines.Contracts;
// using InstanceService.Api.Utilities;
// using InstanceService.Api.Utilities.Provider;
// using Messaging.Core.Abstractions;
// using Newtonsoft.Json;
//
// namespace InstanceService.Api.Messaging.Consumers.Guidelines
// {
//     /// <summary>
//     /// Consumer for creating reduced guideline for a given usecase and user group.
//     /// </summary>
//     public class CreateReducedGuidelineConsumer(ILogger<IInternalRequestConsumer<CreateReducedGuideline, CreateReducedGuidelineResponse>> logger,
//         IMinioHelper minioHelper,
//         AccessRightsFetcher accessRightsFetcher,
//         IGuidelineProvider guidelineProvider) : IInternalRequestConsumer<CreateReducedGuideline, CreateReducedGuidelineResponse>
//     {
//         public ILogger<IInternalRequestConsumer<CreateReducedGuideline, CreateReducedGuidelineResponse>> Logger { get; } = logger;
//
//         private readonly IMinioHelper _minioHelper = minioHelper;
//         private readonly AccessRightsFetcher _accessRightsFetcher = accessRightsFetcher;
//         private readonly IGuidelineProvider _guidelineProvider = guidelineProvider;
//
//         public async Task<CreateReducedGuidelineResponse> ConsumeInternal(CreateReducedGuideline request)
//         {
//             Logger.LogInformation("Creating reduced guideline for usecase {UseCase}", request.UseCaseId);
//
//             var accessRights = await _accessRightsFetcher.GetAccessRightsByUseCaseUserGroupAsync(request.UserGroupId, request.UseCaseId);
//
//             if (accessRights == null || !accessRights.Any())
//             {
//                 Logger.LogError("No access rights found for user group {UserGroup} and usecase {UseCase}",
//                     request.UserGroupId, request.UseCaseId);
//                 return null;
//             }
//
//             var guideline = await _guidelineProvider.GetGuideline(request.UseCaseId);
//
//             if (guideline == null)
//             {
//                 Logger.LogError("No guideline found for usergroup {UserGroup} and usecase {UseCase}",
//                     request.UserGroupId, request.UseCaseId);
//                 return null;
//             }
//
//             var reducedGuideline = GuidelineHelper.GetReducedGuideline(Logger, guideline, accessRights);
//
//             var reducedGuidelineJson = JsonConvert.SerializeObject(reducedGuideline, GuidelineReaderWriter.GetSettings()) ?? "";
//
//             // Object key / bucket previously came from GuidelineOptions (now removed); supply them here
//             // when this is reinstated in the dedicated service.
//             var objectName = $"{request.UserGroupId}/{request.UseCaseId}/guideline.json";
//
//             await _minioHelper.UploadJsonAsync("guideline", objectName, reducedGuidelineJson);
//
//             var url = await _minioHelper.GetObjectUrl("guideline", objectName);
//
//             return new CreateReducedGuidelineResponse() { Url = url };
//         }
//     }
// }
