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

import type { IOutputFormatter } from './IOutputFormatter';
import type { Type } from './Type';

export type CreatedResult = {
    value?: any;
    formatters?: Array<IOutputFormatter> | null;
    contentTypes?: Array<string> | null;
    declaredType?: Type;
    statusCode?: number | null;
    location?: string | null;
};
