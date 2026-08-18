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
import type { InstanceService_Api_Dto_Request_CreateInstance_CreateInstanceWithRelation } from './InstanceService_Api_Dto_Request_CreateInstance_CreateInstanceWithRelation';
/**
 * Data transfer object for creating a node with optional a relation.
 */
export type InstanceService_Api_Dto_Request_CreateInstance = {
    /**
     * The name the node should have.
     */
    name: string;
    /**
     * The classification id the node should have.
     */
    classificationId?: string | null;
    /**
     * The key value pairs that should be added as attributes to the node.
     */
    properties?: Record<string, string> | null;
    relation?: InstanceService_Api_Dto_Request_CreateInstance_CreateInstanceWithRelation;
};

