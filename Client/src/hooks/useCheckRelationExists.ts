// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useCallback } from 'react';
import { enqueueSnackbar, VariantType } from 'notistack';
import { InstanceService_Api_Dto_InstanceRelation as InstanceRelation } from '../services/instance';

interface UseCheckRelationExistsParams {
	relations: InstanceRelation[] | null | undefined;
}

/**
 * Hook to check if a relation already exists between two nodes.
 * Shows a snackbar notification if the relation exists.
 */
export const useCheckRelationExists = ({ relations }: UseCheckRelationExistsParams) => {
	const checkRelationExists = useCallback(
		(
			subjectId: string | null | undefined,
			objectId: string | null | undefined,
			predicateUri: string | null | undefined,
			variant?: VariantType
		): boolean => {
			if (!subjectId || !predicateUri || !objectId) return false;

			const relationExists = relations?.some(
				(r) =>
					r.predicateUri === predicateUri &&
					r.subjectId === subjectId &&
					r.objectId === objectId
			);

			if (relationExists) {
				if(variant) enqueueSnackbar('Relation exists', { variant });
				return true;
			}

			return false;
		},
		[relations]
	);

	return { checkRelationExists };
};
