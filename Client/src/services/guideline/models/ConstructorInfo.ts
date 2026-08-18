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

import type { CallingConventions } from './CallingConventions';
import type { CustomAttributeData } from './CustomAttributeData';
import type { MemberTypes } from './MemberTypes';
import type { MethodAttributes } from './MethodAttributes';
import type { MethodImplAttributes } from './MethodImplAttributes';
import type { Module } from './Module';
import type { RuntimeMethodHandle } from './RuntimeMethodHandle';
import type { Type } from './Type';

export type ConstructorInfo = {
    readonly name?: string | null;
    declaringType?: Type;
    reflectedType?: Type;
    module?: Module;
    readonly customAttributes?: Array<CustomAttributeData> | null;
    readonly isCollectible?: boolean;
    readonly metadataToken?: number;
    attributes?: MethodAttributes;
    methodImplementationFlags?: MethodImplAttributes;
    callingConvention?: CallingConventions;
    readonly isAbstract?: boolean;
    readonly isConstructor?: boolean;
    readonly isFinal?: boolean;
    readonly isHideBySig?: boolean;
    readonly isSpecialName?: boolean;
    readonly isStatic?: boolean;
    readonly isVirtual?: boolean;
    readonly isAssembly?: boolean;
    readonly isFamily?: boolean;
    readonly isFamilyAndAssembly?: boolean;
    readonly isFamilyOrAssembly?: boolean;
    readonly isPrivate?: boolean;
    readonly isPublic?: boolean;
    readonly isConstructedGenericMethod?: boolean;
    readonly isGenericMethod?: boolean;
    readonly isGenericMethodDefinition?: boolean;
    readonly containsGenericParameters?: boolean;
    methodHandle?: RuntimeMethodHandle;
    readonly isSecurityCritical?: boolean;
    readonly isSecuritySafeCritical?: boolean;
    readonly isSecurityTransparent?: boolean;
    memberType?: MemberTypes;
};
