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
import type { InstanceService_Api_Dto_Ontology_RelationDTO } from '../models/InstanceService_Api_Dto_Ontology_RelationDTO';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class OntologyService {
    /**
     * Get relations
     * Returns all relations involving the passed id as subject or object
     * @param objectUri The passed Id whose relations are to be retrieved.
     * @returns InstanceService_Api_Dto_Ontology_RelationDTO OK
     * @throws ApiError
     */
    public static getRelations(
        objectUri: string,
    ): CancelablePromise<Array<InstanceService_Api_Dto_Ontology_RelationDTO>> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/Ontology/{objectUri}',
            path: {
                'objectUri': objectUri,
            },
            errors: {
                400: `Bad Request`,
                401: `Unauthorized`,
            },
        });
    }
}
