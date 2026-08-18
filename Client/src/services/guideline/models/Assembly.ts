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

import type { CustomAttributeData } from './CustomAttributeData';
import type { MethodInfo } from './MethodInfo';
import type { Module } from './Module';
import type { SecurityRuleSet } from './SecurityRuleSet';
import type { Type } from './Type';
import type { TypeInfo } from './TypeInfo';

export type Assembly = {
    readonly definedTypes?: Array<TypeInfo> | null;
    readonly exportedTypes?: Array<Type> | null;
    /**
     * @deprecated
     */
    readonly codeBase?: string | null;
    entryPoint?: MethodInfo;
    readonly fullName?: string | null;
    readonly imageRuntimeVersion?: string | null;
    readonly isDynamic?: boolean;
    readonly location?: string | null;
    readonly reflectionOnly?: boolean;
    readonly isCollectible?: boolean;
    readonly isFullyTrusted?: boolean;
    readonly customAttributes?: Array<CustomAttributeData> | null;
    /**
     * @deprecated
     */
    readonly escapedCodeBase?: string | null;
    manifestModule?: Module;
    readonly modules?: Array<Module> | null;
    /**
     * @deprecated
     */
    readonly globalAssemblyCache?: boolean;
    readonly hostContext?: number;
    securityRuleSet?: SecurityRuleSet;
};
