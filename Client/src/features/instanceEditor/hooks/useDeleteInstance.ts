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
import { InstancesService } from '../../../services/instance';
import { Route } from '../../../routes/instancesRoute';

/**
 * Hook to delete an instance/node.
 * @param nodeId - The node ID to delete
 * @param useCaseId - The use case ID
 * @returns Mutation for deleting an instance
 */
export const useDeleteInstance = (nodeId?: string | null, useCaseId?: string | null) => {
	const queryClient = useQueryClient();
	const { textQuery } = Route.useSearch();

	return useMutation({
		mutationFn: () => InstancesService.deleteInstance(useCaseId!, nodeId!),
		onError: (error: any) => {
			const message = error?.body?.detail ?? error?.message ?? 'Error deleting node';
			enqueueSnackbar(message, { variant: 'error' });
		},
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['nodesGraph', useCaseId] });
			queryClient.invalidateQueries({ queryKey: ['filteredNodesGraph', useCaseId, textQuery] });
			enqueueSnackbar('Node deleted successfully', { variant: 'success' });
		},
	});
};
