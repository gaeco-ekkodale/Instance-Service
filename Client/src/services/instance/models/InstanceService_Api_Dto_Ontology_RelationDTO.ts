// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
/**
 * Represents a data transfer object for a relation.
 */
export type InstanceService_Api_Dto_Ontology_RelationDTO = {
    /**
     * Gets or sets the subject ID.
     */
    subjectId?: string | null;
    /**
     * Gets or sets the predicate ID — the canonical ontology property URI
     * (e.g. https://ibpdi.org/ontology/2.0/addressHasBuilding). This is the
     * identifier used everywhere internally and in Kafka export/import.
     */
    predicateId?: string | null;
    /**
     * Gets or sets the human-readable relation label (e.g. "Address has Building").
     * For display in the Instance Client only; never used as an identifier.
     */
    label?: string | null;
    /**
     * Gets or sets the object ID.
     */
    objectId?: string | null;
};

