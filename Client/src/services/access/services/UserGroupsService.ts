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
import type { UserGroupDTO } from '../models/UserGroupDTO';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class UserGroupsService {

    /**
     *  API call to get all User Groups from Keycloak.
     * @returns UserGroupDTO OK
     * @throws ApiError
     */
    public static getKeycloakGroups(): CancelablePromise<Array<UserGroupDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/UserGroups',
            errors: {
                404: `Not Found`,
                500: `Internal Server Error`,
            },
        });
    }

    /**
     *  API call to get all User Groups of a User from Keycloak.
     * @param userId 
     * @returns UserGroupDTO OK
     * @throws ApiError
     */
    public static getKeycloakGroupsByUser(
userId: string,
): CancelablePromise<Array<UserGroupDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/UserGroups/user/{userId}',
            path: {
                'userId': userId,
            },
            errors: {
                404: `Not Found`,
                500: `Internal Server Error`,
            },
        });
    }

}
