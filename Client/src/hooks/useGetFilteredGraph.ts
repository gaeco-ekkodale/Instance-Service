// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useQuery } from '@tanstack/react-query';
import { InstanceService_Api_Dto_Graph as Graph, InstancesService } from '../services/instance';

/**
 * Hook to fetch a filtered graph based on a text query.
 * @param useCaseId - The use case ID
 * @param textQuery - The text query to filter the graph
 * @returns Query result containing the filtered graph data
 */
export const useGetFilteredGraph = (useCaseId?: string | null, textQuery?: string | null) => {
	return useQuery({
		queryKey: ['filteredNodesGraph', useCaseId, textQuery],
		queryFn: (): Promise<Graph> =>
			InstancesService.getFilteredGraph(useCaseId ?? '', decodeURIComponent(textQuery ?? '')),
		enabled: !!useCaseId && !!textQuery,
	});
};
