// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useCallback, useEffect, useRef, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate, useLocation } from 'react-router-dom';
import { enqueueSnackbar } from 'notistack';
import {
	InstanceService_Api_Dto_Instance as Instance,
	InstanceService_Api_Dto_InstanceRelation as InstanceRelation,
	InstanceService_Models_Enum_Accessibility as Accessibility,
	InstancesService,
	RelationsService,
} from '../../../../services/instance';
import { Route } from '../../../../routes/instancesRoute';
import { useGetInstance } from '../../../../hooks';

export interface ContextMenuState {
	x: number;
	y: number;
	type: 'node' | 'edge';
	targetInstance?: Instance;
	objectInstance?: Instance;
	relation?: InstanceRelation;
}

interface UseGraphContextMenuProps {
	allInstances: Instance[];
	edgeRelationMap: Map<string, InstanceRelation>;
	useCaseId: string;
	visNetwork: any;
}

export const useGraphContextMenu = ({
	allInstances,
	edgeRelationMap,
	useCaseId,
	visNetwork,
}: UseGraphContextMenuProps) => {
	const queryClient = useQueryClient();
	const navigate = useNavigate();
	const location = useLocation();
	const { textQuery, nodeId } = Route.useSearch();

	const [menuState, setMenuState] = useState<ContextMenuState | null>(null);

	const allInstancesRef = useRef(allInstances);
	allInstancesRef.current = allInstances;
	const edgeRelationMapRef = useRef(edgeRelationMap);
	edgeRelationMapRef.current = edgeRelationMap;
	const visNetworkRef = useRef(visNetwork);
	visNetworkRef.current = visNetwork;
	const nodeIdRef = useRef(nodeId);
	nodeIdRef.current = nodeId;

	const invalidate = useCallback(() => {
		queryClient.invalidateQueries({ queryKey: ['nodesGraph', useCaseId] });
		queryClient.invalidateQueries({ queryKey: ['filteredNodesGraph', useCaseId, textQuery] });
	}, [queryClient, useCaseId, textQuery]);

	const { mutate: deleteInstanceMutate } = useMutation({
		mutationFn: (instanceId: string) =>
			InstancesService.deleteInstance(useCaseId, instanceId),
		onSuccess: (_, instanceId) => {
			invalidate();
			enqueueSnackbar('Node deleted', { variant: 'success' });
			if (instanceId === nodeIdRef.current) {
				const params = new URLSearchParams(location.search);
				params.delete('nodeId');
				navigate({ pathname: location.pathname, search: `?${params.toString()}` });
			}
		},
		onError: (error: any) => {
			const message = error?.body?.detail ?? error?.message ?? 'Error deleting node';
			enqueueSnackbar(message, { variant: 'error' });
		},
	});

	const { mutate: deleteRelationMutate } = useMutation({
		mutationFn: ({ subjectId, objectId, predicateUri }: { subjectId: string; objectId: string; predicateUri: string }) =>
			RelationsService.deleteRelation(useCaseId, subjectId, objectId, predicateUri),
		onSuccess: () => {
			invalidate();
			enqueueSnackbar('Connection removed', { variant: 'success' });
		},
		onError: () => enqueueSnackbar('Error removing connection', { variant: 'error' }),
	});

	const handleContextMenu = useCallback((event: any) => {
		event.event.preventDefault();
		const { pointer } = event;

		// Use getNodeAt/getEdgeAt instead of event.nodes/edges to avoid stale
		// selection state from a previous left-click tainting the right-click target.
		const net = visNetworkRef.current;
		const clickedNodeId = net?.getNodeAt(pointer.DOM);
		const clickedEdgeId = !clickedNodeId ? net?.getEdgeAt(pointer.DOM) : undefined;

		if (clickedNodeId !== undefined && clickedNodeId !== null) {
			const instance = allInstancesRef.current.find(i => i.id === String(clickedNodeId));
			if (instance) {
				setMenuState({ x: pointer.DOM.x, y: pointer.DOM.y, type: 'node', targetInstance: instance });
			}
		} else if (clickedEdgeId !== undefined && clickedEdgeId !== null) {
			const relation = edgeRelationMapRef.current.get(String(clickedEdgeId));
			if (relation) {
				const subject = allInstancesRef.current.find(i => i.id === relation.subjectId);
				const object = allInstancesRef.current.find(i => i.id === relation.objectId);
				setMenuState({ x: pointer.DOM.x, y: pointer.DOM.y, type: 'edge', relation, targetInstance: subject, objectInstance: object });
			}
		} else {
			setMenuState(null);
		}
	}, []);

	useEffect(() => {
		if (!menuState) return;
		const close = () => setMenuState(null);
		document.addEventListener('mousedown', close);
		return () => document.removeEventListener('mousedown', close);
	}, [menuState]);

	const closeMenu = useCallback(() => setMenuState(null), []);

	const handleDeleteNode = useCallback(() => {
		if (!menuState?.targetInstance?.id) return;
		deleteInstanceMutate(menuState.targetInstance.id);
		closeMenu();
	}, [menuState, deleteInstanceMutate, closeMenu]);

	const handleDeleteRelation = useCallback(() => {
		if (!menuState?.relation) return;
		const { subjectId, objectId, predicateUri } = menuState.relation;
		if (!subjectId || !objectId || !predicateUri) return;
		deleteRelationMutate({ subjectId, objectId, predicateUri });
		closeMenu();
	}, [menuState, deleteRelationMutate, closeMenu]);

	const { data: menuNodeData, isLoading: menuNodeLoading } = useGetInstance(
		menuState?.targetInstance?.id ?? null,
		useCaseId
	);

	const menuNodeHasFilledReadOnly = !menuNodeLoading
		&& (menuNodeData?.properties?.some(p => p.isReadOnly === true && !!p.value) ?? false);

	const canDelete = !menuNodeLoading
		&& (menuState?.targetInstance?.accessibility === Accessibility.FullControl
			|| menuState?.targetInstance?.accessibility === Accessibility.ReadWrite)
		&& !menuNodeHasFilledReadOnly;

	return {
		menuState,
		closeMenu,
		canDelete,
		handleDeleteNode,
		handleDeleteRelation,
		handleContextMenu,
	};
};
