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
import type { InstanceService_Api_Dto_Graph } from '../models/InstanceService_Api_Dto_Graph';
import type { InstanceService_Api_Dto_Metadata } from '../models/InstanceService_Api_Dto_Metadata';
import type { InstanceService_Api_Dto_Request_CreateInstance } from '../models/InstanceService_Api_Dto_Request_CreateInstance';
import type { InstanceService_Api_Dto_Request_UpdateInstance } from '../models/InstanceService_Api_Dto_Request_UpdateInstance';
import type { CancelablePromise } from '../core/CancelablePromise';
import { OpenAPI } from '../core/OpenAPI';
import { request as __request } from '../core/request';
export class InstancesService {
    /**
     * Gets the graph of all nodes and relations regarding the use case and user groups.
     * @param useCaseId The id of the use case to filter.
     * @returns InstanceService_Api_Dto_Graph The graph of basic instance information and relations.
     * @throws ApiError
     */
    public static getGraph(
        useCaseId: string,
    ): CancelablePromise<InstanceService_Api_Dto_Graph> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/{useCaseId}/Instances/graph',
            path: {
                'useCaseId': useCaseId,
            },
            errors: {
                401: `Unauthorized`,
            },
        });
    }
    /**
     * Gets the graph of all nodes and relations fitting the query.
     * @param useCaseId The id of the use case to filter.
     * @param query The query used to filter the results.
     * @returns InstanceService_Api_Dto_Graph The graph of basic instance information and relations.
     * @throws ApiError
     */
    public static getFilteredGraph(
        useCaseId: string,
        query?: string,
    ): CancelablePromise<InstanceService_Api_Dto_Graph> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/{useCaseId}/Instances/filteredGraph',
            path: {
                'useCaseId': useCaseId,
            },
            query: {
                'query': query,
            },
            errors: {
                401: `Unauthorized`,
            },
        });
    }
    /**
     * Update a node by id with given data.
     * @param useCaseId The use case ID.
     * @param instanceId The id of the node to update.
     * @param requestBody The data to update the node with.
     * @returns void
     * @throws ApiError
     */
    public static updateInstance(
        useCaseId: string,
        instanceId: string,
        requestBody?: InstanceService_Api_Dto_Request_UpdateInstance,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'PUT',
            url: '/{useCaseId}/Instances/{instanceId}',
            path: {
                'useCaseId': useCaseId,
                'instanceId': instanceId,
            },
            body: requestBody,
            mediaType: 'application/json',
            errors: {
                401: `Unauthorized`,
            },
        });
    }
    /**
     * Get the metadata of a node by id.
     * @param instanceId The id of the node to get metadata from.
     * @param useCaseId The id of the use case to filter.
     * @returns InstanceService_Api_Dto_Metadata The metadata of the node.
     * @throws ApiError
     */
    public static getInstance(
        instanceId: string,
        useCaseId: string,
    ): CancelablePromise<InstanceService_Api_Dto_Metadata> {
        return __request(OpenAPI, {
            method: 'GET',
            url: '/{useCaseId}/Instances/{instanceId}',
            path: {
                'instanceId': instanceId,
                'useCaseId': useCaseId,
            },
            errors: {
                401: `Unauthorized`,
            },
        });
    }
    /**
     * Deletes an instance with the specified Id.
     * @param useCaseId The use case ID.
     * @param instanceId The id of the instance to be deleted.
     * @returns void
     * @throws ApiError
     */
    public static deleteInstance(
        useCaseId: string,
        instanceId: string,
    ): CancelablePromise<void> {
        return __request(OpenAPI, {
            method: 'DELETE',
            url: '/{useCaseId}/Instances/{instanceId}',
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
     * Create an instance node with given data.
     * @param useCaseId The use case ID.
     * @param requestBody The data to create the node with.
     * @returns string The node was created successfully with the id to identify the created instance.
     * @throws ApiError
     */
    public static createInstance(
        useCaseId: string,
        requestBody?: InstanceService_Api_Dto_Request_CreateInstance,
    ): CancelablePromise<string> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/{useCaseId}/Instances',
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
     * Get the metadata of multiple Nodes by id.
     * @param useCaseId The id of the use case to filter.
     * @param requestBody The ids of the nodes to get metadata from, provided in the request body.
     * @returns InstanceService_Api_Dto_Metadata The metadata of the nodes inside a enumerable.
     * @throws ApiError
     */
    public static getInstancesMetadata(
        useCaseId: string,
        requestBody?: Array<string>,
    ): CancelablePromise<Array<InstanceService_Api_Dto_Metadata>> {
        return __request(OpenAPI, {
            method: 'POST',
            url: '/{useCaseId}/Instances/metadata',
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
}
