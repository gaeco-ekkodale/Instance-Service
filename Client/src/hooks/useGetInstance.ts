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
import { InstancesService } from '../services/instance';

/**
 * Hook to fetch a single instance's metadata.
 * @param nodeId - The node/instance ID
 * @param useCaseId - The use case ID
 * @returns Query result containing the instance metadata
 */
export const useGetInstance = (nodeId?: string | null, useCaseId?: string | null) => {
	return useQuery({
		queryKey: ['nodeMetaData', nodeId],
		queryFn: () => InstancesService.getInstance(nodeId!, useCaseId!),
		enabled: !!nodeId && !!useCaseId,
	});
};
