// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Models.Enum;

namespace InstanceService.Models;

/// <summary>
/// Represents an Access Right.
/// Originates from Access.Data.
/// </summary>
public class AccessRight
{
    /// <summary>
    /// Id of the AccessRight
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// Name of the AccessRight (Classification Property name)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Id of the GuidelineClassification 
    /// </summary>
    public string? GuidelineClassificationId { get; set; }

    /// <summary>
    /// Id of the Usergroup the right belongs to 
    /// </summary>
    public Guid UserGroupId { get; set; }

    /// <summary>
    /// Id of the Use Case the Access right belongs to
    /// </summary>
    public Guid UseCaseId { get; set; }

    /// <summary>
    /// Id of the Guideline Classification 
    /// </summary>
    public string? GuidlineClassificationPropertyId { get; set; }

    /// <summary>
    /// The Right of the Accessright
    /// </summary>
    public PropertyRight Right { get; set; }
}