// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box } from '@mui/material';
import { useEffect } from 'react';
import EmptyState from '../components/EmptyState';
import { InstancesProvider } from '../features/instances/InstancesProvider';
import { useInstances } from '../features/instances/instancesContext';
import AppHeader from '../features/shell/AppHeader';
import Tour from '../features/tour/Tour';
import { TOUR_KEY, TOUR_MODULE_NAME, TOUR_PANELS } from '../features/tour/tourContent';
import { GraphView } from '../features/views/graph/GraphView';
import { TableView } from '../features/views/table/TableView';

/**
 * Both views of the instances, side by side in the DOM and switched by the header. They stay
 * mounted, so switching keeps the graph layout as well as the table's sorting and filters.
 */
function Views() {
	const { useCaseId, isNodeView, setCreateNodeMode } = useInstances();

	// Leaving the canvas ends a half-finished create, which the table cannot continue.
	useEffect(() => {
		if (!isNodeView) setCreateNodeMode(false);
	}, [isNodeView, setCreateNodeMode]);

	if (!useCaseId)
		return (
			<Box className="flex flex-1 justify-center pt-40">
				<EmptyState
					title="Browse and edit your data"
					description="Pick a UseCase in the toolbar above to open its graph."
				/>
			</Box>
		);

	return (
		<Box className="relative min-h-0 flex-1">
			<Box className={`h-full ${isNodeView ? '' : 'hidden'}`}>
				<GraphView />
			</Box>
			<Box className={`h-full p-3 ${isNodeView ? 'hidden' : ''}`}>
				<TableView />
			</Box>
		</Box>
	);
}

function InstancesPage() {
	return (
		<InstancesProvider>
			{/* Fixed to the bottom right of the viewport; the panel is non-modal, so the
			    tutorial can be followed while actually clicking through the graph. */}
			<Tour tourKey={TOUR_KEY} moduleName={TOUR_MODULE_NAME} panels={TOUR_PANELS} />

			{/* The host navigation takes the first 4rem of the viewport. */}
			<Box className="flex h-[calc(100vh-4rem)] flex-col">
				<AppHeader />
				<Views />
			</Box>
		</InstancesProvider>
	);
}

export default InstancesPage;
