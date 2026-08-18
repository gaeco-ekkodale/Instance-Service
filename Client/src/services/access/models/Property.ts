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

import type { PropertyRight } from './PropertyRight';
import type { StorageType } from './StorageType';

export type Property = {
    id?: string | null;
    name?: string | null;
    value?: string | null;
    storageType?: StorageType;
    right?: PropertyRight;
    [key: string]: any;
};
