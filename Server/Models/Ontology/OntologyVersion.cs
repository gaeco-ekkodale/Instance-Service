// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace InstanceService.Models.Ontology;

public class OntologyVersion
{
    /// <summary>
    /// Primary key. The OntologyService's stable ontology GUID, taken directly from the upload/delete
    /// event so the identifier is the same across services and re-uploads replace the existing version.
    /// </summary>
    public Guid Id { get; set; }
    public string Etag { get; set; } = string.Empty;
    public DateTimeOffset LoadedAt { get; set; }
}
