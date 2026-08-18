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
import type { InstanceService_Models_Enum_Accessibility } from './InstanceService_Models_Enum_Accessibility';
/**
 * The node of a graph.
 */
export type InstanceService_Api_Dto_Instance = {
    /**
     * The id of the node.
     */
    id?: string | null;
    /**
     * The name of the node.
     */
    name?: string | null;
    /**
     * The id of the classification of the node.
     */
    classificationId?: string | null;
    /**
     * The name of the classification of the node.
     */
    classificationName?: string | null;
    /**
     * The name of the guideline that defines the classification.
     */
    guidelineName?: string | null;
    accessibility?: InstanceService_Models_Enum_Accessibility;
};

