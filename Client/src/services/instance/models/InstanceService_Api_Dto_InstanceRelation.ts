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
 * The relation between two nodes with its predicate URI and the label resolved for display.
 * Read model only; use InstanceService.Api.Dto.Request.CreateRelation to create relations.
 */
export type InstanceService_Api_Dto_InstanceRelation = {
    /**
     * The id of the node, which are the subject of the relation.
     */
    subjectId?: string | null;
    /**
     * The id of the node, which are the object of the relation.
     */
    objectId?: string | null;
    /**
     * The canonical ontology property URI identifying the relation.
     * This is the value used everywhere internally.
     */
    predicateUri?: string | null;
    /**
     * The human-readable label of the relation, resolved from the ontology for display only.
     * Populated by the service on read (GET).
     */
    label?: string | null;
};

