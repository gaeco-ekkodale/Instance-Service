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
import type { InstanceService_Api_Dto_Request_CreateRelation } from '../models/InstanceService_Api_Dto_Request_CreateRelation';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class RelationsService {
    /**
     * Create a single relation between two nodes.
     * Subject - has Relation -> Object
     * @param useCaseId The use case ID.
     * @param requestBody The relation to create, identified by the subject id, the object id and the canonical ontology property URI.
     * @returns void
     * @throws ApiError
     */
    public static createRelation(
        useCaseId: string,
        requestBody?: InstanceService_Api_Dto_Request_CreateRelation,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/{useCaseId}/Instances/relation',
            path: {
                'useCaseId': useCaseId,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                401: `Unauthorized`,
            },
        });
    }
    /**
     * Create multiple relations between nodes based on triples of subject id, object id and predicate URI.
     * @param useCaseId The use case ID.
     * @param requestBody The relations to create.
     * @returns void
     * @throws ApiError
     */
    public static createRelations(
        useCaseId: string,
        requestBody?: Array<InstanceService_Api_Dto_Request_CreateRelation>,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/{useCaseId}/Instances/relations',
            path: {
                'useCaseId': useCaseId,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                401: `Unauthorized`,
            },
        });
    }
    /**
     * Deletes all relations of the instance with the specified id.
     * @param useCaseId The use case ID.
     * @param instanceId The id of the instance, whose relations are to be deleted.
     * @returns void
     * @throws ApiError
     */
    public static deleteRelations(
        useCaseId: string,
        instanceId: string,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/{useCaseId}/Instances/{instanceId}/relations',
            path: {
                'useCaseId': useCaseId,
                'instanceId': instanceId,
            },
            errors: {
                401: `Unauthorized`,
            },
        });
    }
    /**
     * Deletes a relation of the two instances with the specified label.
     * Subject - has Relation -> Object
     * @param useCaseId The id of the usecase, in which the relation is to be deleted.
     * @param instanceId The id of the subject instance that uses the relation
     * @param objectId The id of the object instance that uses the relation
     * @param predicateUri The canonical ontology property URI identifying the relation
     * @returns void
     * @throws ApiError
     */
    public static deleteRelation(
        useCaseId: string,
        instanceId: string,
        objectId?: string,
        predicateUri?: string,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/{useCaseId}/Instances/{instanceId}/relation',
            path: {
                'useCaseId': useCaseId,
                'instanceId': instanceId,
            },
            query: {
                'objectId': objectId,
                'predicateUri': predicateUri,
            },
            errors: {
                401: `Unauthorized`,
            },
        });
    }
}
