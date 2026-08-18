// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useMutation, useQueryClient } from '@tanstack/react-query';
import { enqueueSnackbar } from 'notistack';
import { InstanceService_Api_Dto_Request_CreateInstance as CreateInstance, InstancesService } from '../../../services/instance';
import { Route } from '../../../routes/instancesRoute';

/**
 * Hook to create a new instance/node.
 *
 * The graph is refetched here rather than left to the change notification: that one is coalesced
 * over a window server side, and the node has to appear under the cursor that placed it.
 *
 * @param useCaseId - The use case ID
 * @returns Mutation for creating an instance
 */
export const useCreateInstance = (useCaseId?: string | null) => {
	const queryClient = useQueryClient();
	const { textQuery } = Route.useSearch();

	return useMutation({
		mutationFn: (newNode: CreateInstance) => {
			if (newNode.properties) {
				newNode.properties = Object.entries(newNode.properties).reduce((acc, [key, value]) => {
					acc[key] = String(value);
					return acc;
				}, {} as Record<string, string>);
			}
			return InstancesService.createInstance(useCaseId!, newNode);
		},
		onError: () => {
			enqueueSnackbar('Error creating new node', { variant: 'error' });
		},
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['nodesGraph', useCaseId] });
			queryClient.invalidateQueries({ queryKey: ['filteredNodesGraph', useCaseId, textQuery] });
			enqueueSnackbar('Node created successfully', { variant: 'success' });
		},
	});
};
