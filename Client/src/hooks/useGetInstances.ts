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
 * Hook to fetch multiple instances' metadata.
 * @param useCaseId - The use case ID
 * @param instanceIds - Array of instance IDs to fetch
 * @returns Query result containing the instances metadata
 */
export const useGetInstances = (useCaseId?: string | null, instanceIds?: string[]) => {
	return useQuery({
		queryKey: ['nodesMetaData', instanceIds, useCaseId],
		queryFn: () => InstancesService.getInstancesMetadata(useCaseId!, instanceIds),
		enabled: !!useCaseId && instanceIds?.length! > 0,
	});
};
