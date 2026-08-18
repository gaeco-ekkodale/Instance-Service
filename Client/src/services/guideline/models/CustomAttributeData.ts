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

import type { ConstructorInfo } from './ConstructorInfo';
import type { CustomAttributeNamedArgument } from './CustomAttributeNamedArgument';
import type { CustomAttributeTypedArgument } from './CustomAttributeTypedArgument';
import type { Type } from './Type';

export type CustomAttributeData = {
    attributeType?: Type;
    constructor?: ConstructorInfo;
    readonly constructorArguments?: Array<CustomAttributeTypedArgument> | null;
    readonly namedArguments?: Array<CustomAttributeNamedArgument> | null;
};
