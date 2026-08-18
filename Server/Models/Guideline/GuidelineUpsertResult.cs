// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace InstanceService.Models.Guideline;

/// <summary>
/// Carries the side effects of a guideline upsert out of the repository so callers can react to them.
/// In the InstanceService the removed classification IDs drive deletion of the graph instances that
/// were created for classes which no longer exist in the (changed) guideline.
/// </summary>
/// <param name="RemovedClassificationIds">
/// Classification IDs that existed in the previous projection but are absent from the new guideline.
/// </param>
/// <param name="RemovedClassificationPropertyIds">
/// Classification-property IDs that were removed from classifications that still exist.
/// </param>
public record GuidelineUpsertResult(
    IReadOnlyList<string> RemovedClassificationIds,
    IReadOnlyList<string> RemovedClassificationPropertyIds);
