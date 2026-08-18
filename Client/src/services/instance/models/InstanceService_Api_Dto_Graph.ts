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
import type { InstanceService_Api_Dto_Instance } from './InstanceService_Api_Dto_Instance';
import type { InstanceService_Api_Dto_InstanceRelation } from './InstanceService_Api_Dto_InstanceRelation';
/**
 * Data transfer object for a graph.
 */
export type InstanceService_Api_Dto_Graph = {
    /**
     * The nodes in a graph.
     */
    instances?: Array<InstanceService_Api_Dto_Instance> | null;
    /**
     * The relations in a graph.
     */
    relations?: Array<InstanceService_Api_Dto_InstanceRelation> | null;
};

