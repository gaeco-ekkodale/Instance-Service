// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/* generated using openapi-typescript-codegen -- do not edit */
/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
import type { InstanceService_Models_Classification } from '../models/InstanceService_Models_Classification';
import type { InstanceService_Models_ClassificationsListSet } from '../models/InstanceService_Models_ClassificationsListSet';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class ClassificationsService {
    /**
     * Retrieve classifications. Filtered by access rights.
     * An Endpoint to retrieve all classifications that the user has access to.
     * @param useCaseId
     * @returns InstanceService_Models_ClassificationsListSet OK
     * @throws ApiError
     */
    public static getClassificationsByUseCaseUserGroup(
        useCaseId: string,
    ): CancelablePromise<InstanceService_Models_ClassificationsListSet> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Classifications/usecase/{useCaseId}',
            path: {
                'useCaseId': useCaseId,
            },
            errors: {
                401: `Unauthorized`,
                404: `Not Found`,
                500: `Internal Server Error`,
            },
        });
    }
    /**
     * Retrieve classification with properties. Filtered by access rights.
     * An Endpoint to retrieve a classification with properties that the user has access to.
     * @param useCaseId The useCase to filter by.
     * @param classificationId The Id of the classification to be retrieved.
     * @returns InstanceService_Models_Classification OK
     * @throws ApiError
     */
    public static getClassificationByUseCaseUserGroup(
        useCaseId: string,
        classificationId: string,
    ): CancelablePromise<InstanceService_Models_Classification> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/api/Classifications/usecase/{useCaseId}/classification/{classificationId}',
            path: {
                'useCaseId': useCaseId,
                'classificationId': classificationId,
            },
            errors: {
                401: `Unauthorized`,
                404: `Not Found`,
                500: `Internal Server Error`,
            },
        });
    }
}
