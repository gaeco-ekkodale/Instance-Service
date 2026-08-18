// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box } from '@mui/material'
import { useDeferredValue, useMemo } from 'react'
import LoadingSpinner from '../../../components/feedback/LoadingSpinner'
import { useInstances } from '../../instances/instancesContext'
import NodeConfigurator from './configurator/NodeConfigurator'
import VisGraphViewer from './components/VisGraphViewer'
import { useGraphClickHandler } from './hooks/useGraphClickHandler'
import { applyPendingNames } from './pendingNames'

/**
 * The instances of the use case as a canvas. Reads them from the shared context, so it shows
 * the same values as the table, including the ones that are not saved yet.
 */
export const GraphView = () => {
	const {
		isNodeView,
		useCaseId,
		nodeId,
		textQuery,
		graph,
		graphIsLoading,
		filteredGraph,
		filteredGraphIsLoading,
		instanceData,
		createNodeMode,
		setCreateNodeMode,
		targetNodeId,
		setTargetNodeId,
		connectableClassIds,
		navigateToNode,
		edits,
	} = useInstances()

	const { handleGraphClick } = useGraphClickHandler({
		useCaseId,
		createNodeMode,
		setTargetNodeId,
		navigateToNode,
	})

	// Deferred, because typing a name rebuilds every node of the graph.
	const graphData = useDeferredValue(
		useMemo(() => applyPendingNames(filteredGraph ?? graph, edits), [filteredGraph, graph, edits]),
	)

	const isBusy = graphIsLoading || filteredGraphIsLoading
	// The use case is loaded but holds nothing to show. Distinguished from a query that
	// simply matched nothing.
	const hasNoInstances = !isBusy && !!graph && instanceData.length === 0

	// There is nothing to lay out before a use case is picked.
	if (!useCaseId) return null

	return (
		<>
			{/* Only while showing: its dialogs render into a portal, which hiding the container
			    would not reach, and their focus trap would fight the table's. */}
			{isNodeView && (
				<Box className="absolute left-4 top-4 z-20">
					<NodeConfigurator
						setCreateNodeMode={setCreateNodeMode}
						createNodeMode={createNodeMode}
						targetNodeId={targetNodeId}
						setTargetNodeId={setTargetNodeId}
					/>
				</Box>
			)}

			{hasNoInstances && (
				<Box className="pointer-events-none absolute inset-x-0 top-16 z-10 flex justify-center">
					<Box className="max-w-sm px-6 text-center">
						<p className="text-sm font-medium text-gray-400">
							{textQuery ? 'No matches for this query' : 'No data in this UseCase yet'}
						</p>
						<p className="mt-1 text-xs text-gray-400">
							{textQuery
								? 'Clear the query to see the full graph again.'
								: 'Click + on the left, then click anywhere on the canvas.'}
						</p>
					</Box>
				</Box>
			)}

			{/* Rendered without data while loading: unmounting would destroy the vis-network
			    instance and reset zoom, pan and node positions. */}
			<VisGraphViewer
				graphData={graphData}
				onClickGraph={handleGraphClick}
				useCaseId={useCaseId}
				createNodeMode={createNodeMode}
				connectableClassIds={connectableClassIds}
				pendingInstanceIds={edits.editedInstanceIds}
			/>

			{/* What creator mode offers is not visible on the canvas itself, least of all that
			    it is where relations are drawn. The hint follows how far along the user is. */}
			{createNodeMode && (
				<Box className="pointer-events-none absolute inset-x-0 bottom-4 z-20 flex justify-center">
					<Box className="rounded-full bg-gray-900/85 px-4 py-1.5 text-xs text-white shadow-lg">
						{nodeId
							? 'Click a second instance to relate the two, or an empty area for a new instance connected to this one.'
							: 'Click an empty area to create an instance, or click an instance to start a relation.'}
					</Box>
				</Box>
			)}

			{/* Laid over the canvas rather than after it: a spinner of its own height would add
			    to the page and sit below the fold, where nobody sees it. */}
			{isBusy && <LoadingSpinner fullscreen={false} overlay={true} />}
		</>
	)
}
