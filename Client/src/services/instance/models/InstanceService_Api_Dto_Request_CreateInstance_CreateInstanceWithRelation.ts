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
import type { InstanceService_Models_Enum_Direction } from './InstanceService_Models_Enum_Direction';
/**
 * The data of the initial relation for a create node request.
 */
export type InstanceService_Api_Dto_Request_CreateInstance_CreateInstanceWithRelation = {
    /**
     * The canonical ontology property URI identifying the relation.
     */
    predicateUri?: string | null;
    /**
     * The node id of the other node in the relation.
     */
    instanceId?: string | null;
    direction?: InstanceService_Models_Enum_Direction;
};

