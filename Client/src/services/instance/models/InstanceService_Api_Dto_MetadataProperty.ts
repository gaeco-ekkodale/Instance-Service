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
import type { InstanceService_Api_Dto_MetadataPropertyEnumValue } from './InstanceService_Api_Dto_MetadataPropertyEnumValue';
/**
 * The property of a metadata object
 */
export type InstanceService_Api_Dto_MetadataProperty = {
    /**
     * The id of the property
     */
    id?: string | null;
    /**
     * The name of the property
     */
    name?: string | null;
    /**
     * The value of the property in an instance
     */
    value?: string | null;
    /**
     * The name of the property set that the property belongs to
     */
    propertySetName?: string | null;
    /**
     * Indicate if the property can be edited or not
     */
    isReadOnly?: boolean;
    storageType?: Guideline_Model_Enums_StorageType;
    /**
     * Discriminator for the form input widget: "PropertySimple", "PropertyEnum", "PropertySuperEnum", "PropertyTree"
     */
    propertyType?: string | null;
    /**
     * Available enum options for PropertyEnum and PropertySuperEnum properties
     */
    enumValues?: Array<InstanceService_Api_Dto_MetadataPropertyEnumValue> | null;
    /**
     * Lower bound constraint for PropertySimple (nullable)
     */
    min?: string | null;
    /**
     * Upper bound constraint for PropertySimple (nullable)
     */
    max?: string | null;
};

