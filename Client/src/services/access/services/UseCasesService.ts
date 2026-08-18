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
import type { UseCase } from '../models/UseCase';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class UseCasesService {

    /**
     * Retrieve use cases. Filtered by access rights.
     * An Endpoint to retrieve all use cases that the user has access to.
     * @returns UseCase OK
     * @throws ApiError
     */
    public static getUseCasesAsync(): CancelablePromise<Array<UseCase>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/UseCases',
            errors: {
                404: `Not Found`,
                500: `Internal Server Error`,
            },
        });
    }

}
