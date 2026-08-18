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
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';

export class OntologyService {

    /**
     * Uploads Turtle-file
     * Uploads ontology turtle-file and overwrites existing file.
     * @param id 
     * @param formData 
     * @returns any Created
     * @throws ApiError
     */
    public static uploadOntologyAsTurtle(
id?: string,
formData?: {
file?: Blob;
},
): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/Ontology/turtle',
            query: {
                'id': id,
            },
            formData: formData,
            mediaType: 'multipart/form-data',
            errors: {
                400: `Bad Request`,
            },
        });
    }

    /**
     * Upload RDF
     * Uploads ontology rdf-file and overwrites existing file.
     * @param id 
     * @param formData 
     * @returns any Created
     * @throws ApiError
     */
    public static uploadOntologyAsRdf(
id?: string,
formData?: {
file?: Blob;
},
): CancelablePromise<any> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/Ontology/rdf',
            query: {
                'id': id,
            },
            formData: formData,
            mediaType: 'multipart/form-data',
            errors: {
                400: `Bad Request`,
            },
        });
    }

}
