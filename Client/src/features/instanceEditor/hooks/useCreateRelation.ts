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
import { InstanceService_Api_Dto_Request_CreateRelation as CreateRelation, RelationsService } from '../../../services/instance';
import { Route } from '../../../routes/instancesRoute';

/**
 * Hook to create a single relation between two instances.
 * @param useCaseId - The use case ID
 * @returns Mutation for creating one relation
 */
export const useCreateRelation = (useCaseId?: string | null) => {
	const queryClient = useQueryClient();
	const { textQuery } = Route.useSearch();

	return useMutation({
		mutationFn: (relation: CreateRelation) => RelationsService.createRelation(useCaseId!, relation),
		onError: () => {
			enqueueSnackbar('Error creating relation', { variant: 'error' });
		},
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['nodesGraph', useCaseId] });
			queryClient.invalidateQueries({ queryKey: ['filteredNodesGraph', useCaseId, textQuery] });
			enqueueSnackbar('Connection created successfully', { variant: 'success' });
		},
	});
};
