// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useState, useCallback } from 'react'
import { Edge, GraphData, IdType, Node as VisNode } from 'react-vis-graph-wrapper'
import {
	InstanceService_Models_Enum_Accessibility as Accessibility,
	InstanceService_Api_Dto_Instance as Instance,
	RelationsService,
	InstanceService_Api_Dto_InstanceRelation as InstanceRelation,
	InstanceService_Api_Dto_Request_CreateRelation as CreateRelation,
} from '../../../../services/instance'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { enqueueSnackbar } from 'notistack'
import { NodePositions } from '../utils/nodePositionStore'

export const useGraphData = (useCaseId: string) => {
	const queryClient = useQueryClient()
	const [graph, setGraph] = useState<GraphData>({
		nodes: [],
		edges: [],
	})
	const [edgeRelationMap, setEdgeRelationMap] = useState<Map<string, InstanceRelation>>(new Map())

	const { mutate: addConnections } = useMutation({
		mutationFn: (relations: CreateRelation[]) =>
			RelationsService.createRelations(useCaseId, relations),
		onError: () => {
			queryClient.invalidateQueries({ queryKey: ['nodesGraph', useCaseId] })
			enqueueSnackbar('Error adding connections', { variant: 'error' })
		},
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['nodesGraph', useCaseId] })
			enqueueSnackbar('Connections added successfully', { variant: 'success' })
		},
	})

	const initNodes = useCallback((
		data: Instance[],
		positions: NodePositions,
		connectableClassIds?: Set<string> | null,
		sourceNodeId?: string | null,
		pendingInstanceIds?: Set<string> | null,
	) => {
		// vis-network rejects a node without an id and throws while the data set is applied,
		// which takes down the whole canvas rather than just that one node. Anything without a
		// usable id is left out instead.
		const newNodeList: VisNode[] = data.filter(instance => !!instance.id).map(instance => {
			const width = 80
			const nodeLabel = getLabel(instance.name ?? '') ?? ''

			let bgColor = getColor(instance)
			let borderColor = getSecondColor(instance)
			// While connecting, the border says what may be picked, which outranks the mark
			// of an instance holding unsaved values.
			const isPending = !connectableClassIds
				&& !!instance.id
				&& !!pendingInstanceIds?.has(instance.id)

			if (connectableClassIds) {
				const isSource = instance.id === sourceNodeId
				const isConnectable = instance.classificationId
					? connectableClassIds.has(instance.classificationId)
					: false

				if (isSource) {
					borderColor = '#1976d2'
				} else if (isConnectable) {
					borderColor = '#f57c00'
				} else {
					bgColor = '#eeeeee'
					borderColor = '#bdbdbd'
				}
			} else if (isPending) {
				borderColor = '#ed6c02'
			}

			// Nodes without a known position are positioned by vis-network itself.
			const position = instance.id ? positions[instance.id] : undefined

			return {
				id: instance.id as IdType,
				label: nodeLabel,
				title: `${getTitle(instance)}${isPending ? '\nUnsaved changes' : ''}`,
				shape: 'circle',
				widthConstraint: width,
				font: { color: 'black' },
				// Spelled out for both states: vis-data merges node updates, so a field left
				// out here would keep the value of the previous mark.
				borderWidth: isPending ? 3 : 2,
				shapeProperties: { borderDashes: isPending ? [5, 4] : false },
				...(position && { x: position.x, y: position.y }),
				color: {
					border: borderColor,
					background: bgColor,
					hover: { background: bgColor, border: borderColor },
					highlight: { background: bgColor, border: borderColor },
				},
			}
		})
		setGraph(prevGraph => ({
			...prevGraph,
			nodes: newNodeList,
		}))
	}, [])

	const getTitle = (data: Instance) => {
		let title = `${data.guidelineName ? data.guidelineName + ' · ' : ''}${data.classificationName}`
		if (data.name && data.name.length > 35) {
			title = `Name: ${data.name} \n ${title}`
			return title
		}
		return title
	}

	const getLabel = (name: string): string | undefined => {
		if (name && name.length > 35) {
			return `${name.slice(0, 32)}...`
		}
		return name
	}

	const truncateUrl = (url?: string | null) => {
		return url?.split(/[/#]/).filter(Boolean).pop() || url || ''
	}

	// The label is resolved from the ontology on read and may be missing; falling back to the
	// predicate URI keeps the edge named instead of dropping the whole graph on a null label.
	const edgeLabel = (relation: InstanceRelation) =>
		truncateUrl(relation.label ?? relation.predicateUri)

	const initConnections = useCallback((data: InstanceRelation[]) => {
		const newConnectionList: Edge[] = []
		const newMap = new Map<string, InstanceRelation>()
		let edgeIdx = 0
		const processedRelations = new Set<string>()

		data.forEach(relation => {
			const forwardKey = `${relation.subjectId}-${relation.objectId}`
			const reverseKey = `${relation.objectId}-${relation.subjectId}`

			if (processedRelations.has(forwardKey) || processedRelations.has(reverseKey)) {
				return
			}

			const fwdId = `e${edgeIdx++}`
			newConnectionList.push({
				id: fwdId,
				from: relation.subjectId as IdType,
				to: relation.objectId as IdType,
				label: edgeLabel(relation),
				font: { align: 'middle' },
				length: 300,
			})
			newMap.set(fwdId, relation)

			const reciprocalRelation = data.find(
				reciprocal =>
					reciprocal.subjectId === relation.objectId && reciprocal.objectId === relation.subjectId,
			)

			if (reciprocalRelation) {
				const revId = `e${edgeIdx++}`
				newConnectionList.push({
					id: revId,
					from: relation.objectId as IdType,
					to: relation.subjectId as IdType,
					label: edgeLabel(reciprocalRelation),
					smooth: { enabled: true, type: 'curvedCW', roundness: 0.5 },
				})
				newMap.set(revId, reciprocalRelation)
				processedRelations.add(forwardKey)
				processedRelations.add(reverseKey)
			} else {
				processedRelations.add(forwardKey)
			}
		})

		setEdgeRelationMap(newMap)
		setGraph(prevGraph => ({
			...prevGraph,
			edges: newConnectionList,
		}))
	}, [])

	const getColor = (data: Instance) => {
		switch (data.accessibility) {
			case Accessibility.None:
				return '#FAF3F0'
			case Accessibility.ReadOnly:
				return '#f49393'
			case Accessibility.FullControl:
				return '#b0ffbd'
			case Accessibility.ReadWrite:
				return '#fff7b0'
		}
		return '#FAF3F0'
	}

	const getSecondColor = (data: Instance) => {
		switch (data.accessibility) {
			case Accessibility.None:
				return '#FAF3F0'
			case Accessibility.ReadOnly:
				return '#b95f61'
			case Accessibility.FullControl:
				return '#3b8f98'
			case Accessibility.ReadWrite:
				return '#c5bf7b'
		}
		return '#FAF3F0'
	}

	return {
		graph,
		initNodes,
		initConnections,
		setGraph,
		addConnections,
		edgeRelationMap,
	}
}
