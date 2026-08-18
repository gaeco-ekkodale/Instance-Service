// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useCallback, useRef } from 'react';
import { toast } from 'sonner';
import {
	InstanceService_Models_Enum_Accessibility as Accessibility,
	InstanceService_Api_Dto_Instance as Instance,
} from '../../../../services/instance';

interface UseCreateClickHandlerProps {
	allInstances: Instance[];
	createNodeMode: boolean;
	nodeId: string | null;
	onClickGraph: (simpleInstance: Instance | undefined) => void;
	connectableClassIds?: Set<string> | null;
}

interface UseCreateClickHandlerReturn {
	handleClick: (event: any) => void;
}

/**
 * Hook that manages create click event handling for the graph viewer.
 * Handles node selection, accessibility checks, and create node mode logic.
 */
export const useCreateClickHandler = ({
	allInstances,
	createNodeMode,
	nodeId,
	onClickGraph,
	connectableClassIds,
}: UseCreateClickHandlerProps): UseCreateClickHandlerReturn => {
	const allInstancesRef = useRef(allInstances);
	allInstancesRef.current = allInstances;

	/**
	 * Find an instance by its ID from the allInstances array.
	 */
	const findInstanceById = useCallback((id: string): Instance | undefined => {
		return allInstancesRef.current.find((node) => node.id === id);
	}, []);

	/**
	 * Handle click events on the graph.
	 * Manages node selection, accessibility validation, and create node mode behavior.
	 */
	const connectableClassIdsRef = useRef(connectableClassIds);
	connectableClassIdsRef.current = connectableClassIds;

	const handleClick = useCallback(
		(event: any) => {
			const { nodes } = event;

			if (createNodeMode && nodeId) {
				if (nodes.length > 0) {
					const targetNodeData = findInstanceById(nodes[0]);
					if (targetNodeData) {
						if (targetNodeData.accessibility !== Accessibility.FullControl) {
							toast.warning('Cannot create relation from node with restricted access.');
							return;
						}
						if (
							connectableClassIdsRef.current &&
							targetNodeData.classificationId &&
							!connectableClassIdsRef.current.has(targetNodeData.classificationId)
						) {
							return;
						}
						onClickGraph(targetNodeData);
					}
				} else {
					onClickGraph(undefined);
				}
				return;
			}

			if (nodes.length > 0) {
				const selectedNodeData = findInstanceById(nodes[0]);
				if (selectedNodeData) onClickGraph(selectedNodeData);
			} else {
				onClickGraph(undefined);
			}
		},
		[createNodeMode, nodeId, onClickGraph, findInstanceById]
	);

	return { handleClick };
};
