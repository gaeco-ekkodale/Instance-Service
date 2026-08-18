// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useState, useEffect, useMemo, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { useGraphSearchParams, useGetGraph, useGetFilteredGraph, useConnectableClassIds } from '../../hooks'
import { Route } from '../../routes/instancesRoute'

/**
 * The instances of the current use case, the filters they are shown under and the view state
 * both views share. Published by InstancesProvider; the result keeps its identity while
 * nothing in it changed, so consumers of the context only re-render on a real change.
 */
export const useInstanceData = () => {
	// URL search params
	const { useCaseId, nodeId, searchTerm, classificationId, textQuery } = useGraphSearchParams()
	const navigate = useNavigate()

	// Local state
	const [isNodeView, setIsNodeView] = useState<boolean>(true)
	const [createNodeMode, setCreateNodeMode] = useState<boolean>(false)
	const [targetNodeId, setTargetNodeId] = useState<string | undefined | null>(undefined)

	// Queries
	const { data: graph, isLoading: graphIsLoading } = useGetGraph(useCaseId)

	const { connectableClassIds } = useConnectableClassIds(
		useCaseId,
		nodeId,
		createNodeMode && !!nodeId,
	)

	const {
		data: filteredGraph,
		isLoading: filteredGraphIsLoading,
		isSuccess: filteredGraphSuccess,
		isError: filteredGraphError,
	} = useGetFilteredGraph(useCaseId, textQuery)

	// Toast notifications for filtered graph
	useEffect(() => {
		if (textQuery && filteredGraphSuccess) {
			toast.success('Fetched filtered graph successfully')
		} else if (textQuery && filteredGraphError) {
			toast.error('Error fetching filtered graph')
		}
	}, [filteredGraphSuccess, filteredGraphError, textQuery])

	// Derived data
	const instanceData = useMemo(() => {
		return filteredGraph?.instances ?? graph?.instances ?? []
	}, [graph, filteredGraph])

	const instanceDataProperties = useMemo(
		() => instanceData.filter(instance => instance.classificationId === classificationId),
		[instanceData, classificationId],
	)

	// Navigation helper. Keeps the filters of both views, so opening an instance from the
	// table does not drop its classification.
	const navigateToNode = useCallback(
		(nodeId?: string) => {
			if (!useCaseId) return

			const params = new URLSearchParams({ useCaseId })
			if (nodeId) params.set('nodeId', nodeId)
			if (textQuery) params.set('textQuery', textQuery)
			if (classificationId) params.set('classificationId', classificationId)

			navigate({
				pathname: Route.path,
				search: `?${params.toString()}`,
			})
		},
		[useCaseId, textQuery, classificationId, navigate],
	)

	return useMemo(
		() => ({
			// Search params
			useCaseId,
			nodeId,
			searchTerm,
			classificationId,
			textQuery,

			// View state
			isNodeView,
			setIsNodeView,

			// Create node mode
			createNodeMode,
			setCreateNodeMode,
			targetNodeId,
			setTargetNodeId,
			connectableClassIds,

			// Graph data
			graph,
			graphIsLoading,
			filteredGraph,
			filteredGraphIsLoading,

			// Instance data
			instanceData,
			instanceDataProperties,

			// Navigation
			navigateToNode,
		}),
		[
			useCaseId,
			nodeId,
			searchTerm,
			classificationId,
			textQuery,
			isNodeView,
			createNodeMode,
			targetNodeId,
			connectableClassIds,
			graph,
			graphIsLoading,
			filteredGraph,
			filteredGraphIsLoading,
			instanceData,
			instanceDataProperties,
			navigateToNode,
		],
	)
}
