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
import type { Guideline_Model_Enums_StorageType } from './Guideline_Model_Enums_StorageType';
import type { InstanceService_Models_Enum_PropertyRight } from './InstanceService_Models_Enum_PropertyRight';
import type { InstanceService_Models_PropertyEnumValue } from './InstanceService_Models_PropertyEnumValue';
export type InstanceService_Models_Property = {
    id?: string | null;
    name?: string | null;
    value?: string | null;
    storageType?: Guideline_Model_Enums_StorageType;
    right?: InstanceService_Models_Enum_PropertyRight;
    propertyType?: string | null;
    enumValues?: Array<InstanceService_Models_PropertyEnumValue> | null;
};

