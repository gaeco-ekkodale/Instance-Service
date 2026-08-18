// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useEffect, useCallback, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { InstanceService_Api_Dto_Instance as Instance } from '../../../../services/instance';
import { Route } from '../../../../routes/instancesRoute';

interface UseGraphSearchProps {
	visNetwork: any;
	allInstances: Instance[];
	useCaseId: string;
	searchTerm: string | null;
	textQuery: string | null;
}

/**
 * Hook that manages search functionality for the graph viewer.
 * Handles searching for nodes by ID and navigating to selected nodes.
 */
export const useGraphSearch = ({
	visNetwork,
	allInstances,
	useCaseId,
	searchTerm,
	textQuery,
}: UseGraphSearchProps): void => {
	const navigate = useNavigate();

	// Use ref to avoid recreating handleSearch when allInstances changes
	const allInstancesRef = useRef(allInstances);
	allInstancesRef.current = allInstances;

	/**
	 * Search for a node by its ID and navigate to it.
	 * Updates the URL with the found node's ID.
	 */
	const handleSearch = useCallback(
		(term: string | null) => {
			if (!useCaseId || !term) return;

			const found = allInstancesRef.current.filter((node) => node.id !== undefined && node.id === term);

			if (found.length === 0) {
				return;
			}

			// TODO: we are only using the first found node, is this the desired behavior?
			const foundNode = found[0];

			const searchParams = [
				['useCaseId', `${useCaseId}`],
				['searchTerm', `${term}`],
			];
			if (foundNode.id) searchParams.push(['nodeId', `${foundNode.id}`]);
			if (textQuery) searchParams.push(['textQuery', `${textQuery}`]);
			const params = new URLSearchParams(searchParams);

			navigate({
				pathname: Route.path,
				search: `?${params.toString()}`,
			});

			selectNode(foundNode.id!);
		},
		[useCaseId, textQuery, navigate]
	);

	function selectNode(nodeId: string) {
		if (visNetwork) {
			// TODO: we could also select the edges connected to the node
			visNetwork.selectNodes([nodeId], false);
			visNetwork.focus(nodeId, {
				scale: 1.0,
				offset: { x: 0, y: 0 },
				animation: { duration: 500, easingFunction: 'easeInOutQuad' },
			});
		}
	}

	/**
	 * Trigger search when searchTerm changes and visNetwork is available.
	 */
	useEffect(() => {
		if (searchTerm && visNetwork) {
			handleSearch(searchTerm);
		}
	}, [searchTerm, visNetwork, handleSearch]);
};
