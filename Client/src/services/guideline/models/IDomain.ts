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

import type { IClassification } from './IClassification';
import type { IProperty } from './IProperty';
import type { IPropertySet } from './IPropertySet';
import type { Status } from './Status';

export type IDomain = {
    classifications?: Array<IClassification> | null;
    properties?: Array<IProperty> | null;
    propertySets?: Array<IPropertySet> | null;
    definition?: string | null;
    description?: string | null;
    identifier?: string | null;
    name?: string | null;
    status?: Status;
    version?: string | null;
    id?: string | null;
};
