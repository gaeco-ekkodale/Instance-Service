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
import { OntologyService } from '../services/instance/services/OntologyService';

/**
 * Hook to fetch possible relations for a given classification.
 * @param classificationId - The encoded classification ID
 * @returns Query result containing possible relations
 */
export const useGetRelations = (classificationId?: string) => {
	return useQuery({
		queryKey: ['possibleRelations', classificationId],
		queryFn: () => OntologyService.getRelations(classificationId!),
		enabled: !!classificationId,
	});
};
