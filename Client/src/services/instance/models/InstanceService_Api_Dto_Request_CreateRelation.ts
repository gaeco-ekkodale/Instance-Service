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
 * Data transfer object for creating a relation.
 * The relation is identified by its predicate URI; labels are never sent by the client.
 */
export type InstanceService_Api_Dto_Request_CreateRelation = {
    /**
     * The id of the subject node of the relation.
     */
    subjectId: string;
    /**
     * The id of the object node of the relation.
     */
    objectId: string;
    /**
     * The canonical ontology property URI identifying the relation
     * (e.g. https://ibpdi.org/ontology/2.0/addressHasBuilding).
     */
    predicateUri: string;
};

