// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { IClassificationProperty } from './IClassificationProperty';
import type { ParameterLocation } from './ParameterLocation';
import type { ParameterMappingDirection } from './ParameterMappingDirection';
import type { UsageType } from './UsageType';

export type IParameterMapping = {
    sourceParameter?: string | null;
    targetParameter?: IClassificationProperty;
    direction?: ParameterMappingDirection;
    sourceParameterValueType?: string | null;
    isBuiltIn?: boolean;
    isShared?: boolean;
    usageType?: UsageType;
    locationParameter?: ParameterLocation;
    id?: string | null;
};
