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

import type { IPropertyAssignment } from './IPropertyAssignment';
import type { IPropertySet } from './IPropertySet';
import type { Status } from './Status';

export type IClassificationProperty = {
    isReadonly?: boolean;
    isRequired?: boolean;
    defaultValue?: string | null;
    propertyAssignment?: IPropertyAssignment;
    propertySet?: IPropertySet;
    sortNumber?: number;
    reference?: string | null;
    definition?: string | null;
    description?: string | null;
    identifier?: string | null;
    name?: string | null;
    status?: Status;
    version?: string | null;
    id?: string | null;
};
