// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useMemo } from 'react';
import { useGetInstances, useGetRelations } from '.';

/**
 * For a given source node in create mode, computes the set of classification IDs
 * that the source can be connected to according to the ontology.
 * Returns null while loading (caller should treat null as "show all").
 */
export const useConnectableClassIds = (
	useCaseId: string | null | undefined,
	nodeId: string | null | undefined,
	enabled: boolean,
) => {
	const instanceIds = useMemo(
		() => (enabled && nodeId ? [nodeId] : undefined),
		[enabled, nodeId],
	);

	const { data: metaData } = useGetInstances(useCaseId, instanceIds);
	const sourceNode = metaData?.find(n => n.id === nodeId);

	const encodedClassId = sourceNode?.classificationId
		? encodeURIComponent(sourceNode.classificationId)
		: undefined;

	const { data: relations } = useGetRelations(enabled ? encodedClassId : undefined);

	const connectableClassIds = useMemo(() => {
		if (!relations || !sourceNode?.classificationId) return null;
		const srcId = sourceNode.classificationId;
		const ids = new Set<string>();
		relations.forEach(rel => {
			if (rel.subjectId === srcId && rel.objectId) ids.add(rel.objectId);
			if (rel.objectId === srcId && rel.subjectId) ids.add(rel.subjectId);
		});
		return ids;
	}, [relations, sourceNode?.classificationId]);

	return { connectableClassIds };
};
