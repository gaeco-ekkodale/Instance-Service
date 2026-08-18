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
import type { CreatedResult } from '../models/CreatedResult';
import type { Guideline } from '../models/Guideline';
import type { IGuideline } from '../models/IGuideline';

import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class GuidelineService {

    /**
     * Uploads guideline file and overwrites existing file on assets.
     * @param formData 
     * @returns CreatedResult Success.
     * @throws ApiError
     */
    public static putGuideline(
formData?: {
file?: Blob;
},
): CancelablePromise<CreatedResult> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/guideline',
            formData: formData,
            mediaType: 'multipart/form-data',
            errors: {
                400: `Bad request.`,
            },
        });
    }

    /**
     * Deserializes stored guideline file from assets and returns guideline object.
     * @returns IGuideline Success.
     * @throws ApiError
     */
    public static getGuideline(): CancelablePromise<IGuideline> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/guideline',
            errors: {
                400: `Bad request.`,
                404: `Not Found`,
            },
        });
    }

    /**
     * Deserializes stored guideline file from assets and returns guideline object as string.
     * @returns string Success.
     * @throws ApiError
     */
    public static getGuidelineText(): CancelablePromise<string> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/guideline/text',
            errors: {
                400: `Bad request.`,
                404: `Not Found`,
                500: `Internal server error.`,
            },
        });
    }

    /**
     * Saves guideline-object and overwrites existing file on assets.
     * @param requestBody 
     * @returns void 
     * @throws ApiError
     */
    public static putGuidelineSave(
requestBody?: Guideline,
): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/guideline/save',
            body: requestBody,
            mediaType: 'application/json-patch+json',
            errors: {
                400: `Bad Request`,
            },
        });
    }

}
