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
import { ClassificationsService } from '../services/instance';

/**
 * Hook to fetch classification details for a given classification and use case.
 * @param useCaseId - The use case ID
 * @param classificationId - The encoded classification ID
 * @returns Query result containing the classification data
 */
export const useGetClassification = (useCaseId?: string, classificationId?: string) => {
	return useQuery({
		queryKey: ['classification', classificationId, useCaseId],
		queryFn: () => ClassificationsService.getClassificationByUseCaseUserGroup(useCaseId!, classificationId!),
		enabled: !!classificationId && !!useCaseId,
	});
};
