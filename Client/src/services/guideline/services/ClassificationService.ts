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
import type { BatchSearch } from '../models/BatchSearch';
import type { ClassificationDTO } from '../models/ClassificationDTO';
import type { ClassificationPropertyDTO } from '../models/ClassificationPropertyDTO';
import type { PropertySetDTO } from '../models/PropertySetDTO';
import type { SimpleClassificationDTO } from '../models/SimpleClassificationDTO';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class ClassificationService {

    /**
     * API Call to get all Classfications from the Uploaded Guideline
     * @returns SimpleClassificationDTO Success
     * @throws ApiError
     */
    public static getClassification(): CancelablePromise<Array<SimpleClassificationDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/classification',
            errors: {
                404: `Not Found`,
            },
        });
    }

    /**
     * Api Endpoint to get a specific Classification.
     * @param id The Id of the targeted Classification
     * @returns ClassificationDTO Success
     * @throws ApiError
     */
    public static getClassification1(
id: string,
): CancelablePromise<ClassificationDTO> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/classification/{ID}',
            path: {
                'ID': id,
            },
            errors: {
                400: `Bad Request`,
                404: `Not Found`,
            },
        });
    }

    /**
     * Api Endpoint to search with a batch for specific Classifications.
     * @param requestBody The Ids of the targeted Classification
     * @returns BatchSearch Success
     * @throws ApiError
     */
    public static postClassificationBatch(
requestBody?: Array<string>,
): CancelablePromise<BatchSearch> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/classification/batch',
            body: requestBody,
            mediaType: 'application/json-patch+json',
            errors: {
                400: `Bad Request`,
                404: `Not Found`,
            },
        });
    }

    /**
     * Api Endpoint to get the Properties of a specific classification.
     * @param id The Id of the targeted Classification
     * @returns ClassificationPropertyDTO Success
     * @throws ApiError
     */
    public static getClassificationProperties(
id: string,
): CancelablePromise<Array<ClassificationPropertyDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/classification/{ID}/properties',
            path: {
                'ID': id,
            },
            errors: {
                400: `Bad Request`,
                404: `Not Found`,
            },
        });
    }

    /**
     * Api Endpoint to get the Property Sets of a specific classification.
     * @param id The Id of the targeted Classification
     * @returns PropertySetDTO Success
     * @throws ApiError
     */
    public static getClassificationPropertysets(
id: string,
): CancelablePromise<Array<PropertySetDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/classification/{ID}/propertysets',
            path: {
                'ID': id,
            },
            errors: {
                400: `Bad Request`,
                404: `Not Found`,
            },
        });
    }

}
