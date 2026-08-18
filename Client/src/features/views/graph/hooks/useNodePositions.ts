// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useCallback, useEffect, useRef, useState } from 'react'
import { InstanceService_Api_Dto_Instance as Instance } from '../../../../services/instance'
import {
	NodePosition,
	NodePositions,
	loadNodePositions,
	saveNodePositions,
} from '../utils/nodePositionStore'

/** Delay before writing to storage; 'stabilized' and 'dragEnd' can fire in quick succession. */
const SAVE_DEBOUNCE_MS = 400

/** Distance between new nodes that share the same drop point. */
const NEW_NODE_SPREAD = 60

/** Number of new nodes placed per ring around the drop point. */
const NEW_NODES_PER_RING = 6

/**
 * Fans nodes out around a drop point so that several new nodes never stack up.
 */
const spreadAround = (dropPoint: NodePosition, index: number): NodePosition => {
	if (index === 0) return { x: Math.round(dropPoint.x), y: Math.round(dropPoint.y) }

	const angle = ((index - 1) % NEW_NODES_PER_RING) * ((2 * Math.PI) / NEW_NODES_PER_RING)
	const radius = NEW_NODE_SPREAD * (1 + Math.floor((index - 1) / NEW_NODES_PER_RING))

	return {
		x: Math.round(dropPoint.x + Math.cos(angle) * radius),
		y: Math.round(dropPoint.y + Math.sin(angle) * radius),
	}
}

interface UseNodePositionsProps {
	visNetwork: any
	useCaseId?: string | null
}

interface UseNodePositionsReturn {
	/**
	 * Returns the positions to render the given instances with. Instances without an entry
	 * are positioned by vis-network. Call this right before building the node list.
	 */
	resolveNodePositions: (instances: Instance[]) => NodePositions
	/**
	 * Remembers where the canvas was clicked, so the instance created from that click
	 * appears under the cursor.
	 */
	rememberCreatePosition: (canvasPosition: NodePosition) => void
	/**
	 * Lets the solver arrange the whole graph again, replacing the layout that is stored.
	 * It ends by itself once the graph comes to rest.
	 */
	startLayout: () => void
	/** Leaves the nodes wherever the solver has moved them so far. */
	stopLayout: () => void
	/** Whether an arrangement started by startLayout is still running. */
	isLayoutRunning: boolean
}

/**
 * Keeps graph node positions stable across reloads.
 *
 * The positions of the running network are the source of truth. They are mirrored into
 * browser storage whenever the physics simulation settles or a node is dropped, and read
 * back when the use case is opened again.
 *
 * Physics runs only while there are nodes left to place. It is the single most expensive
 * thing on the canvas, and on a graph whose layout is already known it has nothing to do
 * but push the arrangement the user made out of shape.
 */
export const useNodePositions = ({
	visNetwork,
	useCaseId,
}: UseNodePositionsProps): UseNodePositionsReturn => {
	/** Positions of all nodes we know of, including nodes not currently rendered. */
	const positionsRef = useRef<NodePositions>({})
	const loadedUseCaseRef = useRef<string | null | undefined>(undefined)
	const pendingCreatePositionRef = useRef<NodePosition | null>(null)
	const saveTimerRef = useRef<number | null>(null)
	/** Set while the user asked for an arrangement, so data updates do not cut it short. */
	const manualLayoutRef = useRef(false)
	const [isLayoutRunning, setIsLayoutRunning] = useState(false)

	/**
	 * Copies the live node positions of the network into the in-memory map.
	 * @returns The number of nodes the network currently holds.
	 */
	const syncFromNetwork = useCallback((): number => {
		if (!visNetwork) return 0

		let livePositions: Record<string, NodePosition>
		try {
			livePositions = visNetwork.getPositions()
		} catch {
			// The network can already be destroyed while effects are cleaned up.
			return 0
		}

		const ids = Object.keys(livePositions)
		ids.forEach(id => {
			const position = livePositions[id]
			// Rounded to keep the payload small and the node data stable between renders.
			if (Number.isFinite(position?.x) && Number.isFinite(position?.y)) {
				positionsRef.current[id] = { x: Math.round(position.x), y: Math.round(position.y) }
			}
		})

		return ids.length
	}, [visNetwork])

	/** The physics state last applied, and the network it was applied to. */
	const physicsRef = useRef<{ network: unknown; enabled: boolean } | null>(null)

	/**
	 * Switches the physics simulation on or off.
	 *
	 * Applying the option restarts the engine and redraws, so only a real change is passed on -
	 * this is called from every data update. Tracked per network, because a new one starts from
	 * whatever GraphSetup says.
	 */
	const setPhysics = useCallback(
		(enabled: boolean) => {
			if (!visNetwork) return

			const applied = physicsRef.current
			if (applied && applied.network === visNetwork && applied.enabled === enabled) return

			try {
				visNetwork.setOptions({ physics: { enabled } })
				physicsRef.current = { network: visNetwork, enabled }
			} catch {
				// The network can already be destroyed while effects are cleaned up.
			}
		},
		[visNetwork],
	)

	const persist = useCallback(() => {
		if (!useCaseId) return

		syncFromNetwork()
		saveNodePositions(useCaseId, positionsRef.current)
	}, [useCaseId, syncFromNetwork])

	const schedulePersist = useCallback(() => {
		if (saveTimerRef.current !== null) window.clearTimeout(saveTimerRef.current)
		saveTimerRef.current = window.setTimeout(() => {
			saveTimerRef.current = null
			persist()
		}, SAVE_DEBOUNCE_MS)
	}, [persist])

	/**
	 * Hands the graph back to the solver. The flag keeps the data updates that arrive while it
	 * runs from switching it off again, and 'stabilized' clears both once it comes to rest.
	 */
	const startLayout = useCallback(() => {
		manualLayoutRef.current = true
		setIsLayoutRunning(true)
		setPhysics(true)
	}, [setPhysics])

	const stopLayout = useCallback(() => {
		manualLayoutRef.current = false
		setIsLayoutRunning(false)
		setPhysics(false)
		// Stopped by hand, so 'stabilized' will not come to write the result away.
		schedulePersist()
	}, [setPhysics, schedulePersist])

	/**
	 * The middle of what is currently on screen, where nodes go that no click placed.
	 */
	const viewCenter = useCallback((): NodePosition => {
		const center = visNetwork?.getViewPosition()
		return Number.isFinite(center?.x) && Number.isFinite(center?.y)
			? center
			: { x: 0, y: 0 }
	}, [visNetwork])

	/**
	 * Consumes the point new nodes are placed at: the last canvas click, or the center of
	 * the viewport when the node was not created by clicking the canvas.
	 */
	const takeDropPoint = useCallback((): NodePosition => {
		const clicked = pendingCreatePositionRef.current
		pendingCreatePositionRef.current = null

		return clicked ?? viewCenter()
	}, [viewCenter])

	const rememberCreatePosition = useCallback((canvasPosition: NodePosition) => {
		if (!Number.isFinite(canvasPosition?.x) || !Number.isFinite(canvasPosition?.y)) return

		pendingCreatePositionRef.current = { x: canvasPosition.x, y: canvasPosition.y }
	}, [])

	const resolveNodePositions = useCallback(
		(instances: Instance[]): NodePositions => {
			// Every use case has its own layout.
			if (loadedUseCaseRef.current !== useCaseId) {
				loadedUseCaseRef.current = useCaseId
				positionsRef.current = loadNodePositions(useCaseId)
				pendingCreatePositionRef.current = null
			}

			// Whatever physics or the user did so far is what we keep.
			const liveNodeCount = syncFromNetwork()

			const unplacedIds = instances
				.map(instance => instance.id)
				.filter((id): id is string => !!id && !positionsRef.current[id])

			// Unknown nodes are only dropped onto a graph that is already laid out. While the
			// network is still empty they belong to the initial load, which vis-network lays out.
			if (unplacedIds.length > 0 && liveNodeCount > 0) {
				// New nodes land on the last canvas click, which the create flow leaves behind
				// and this consumes. Nodes that arrive without one - an instance another user
				// created, an import - go to the middle of the view.
				const dropPoint = takeDropPoint()

				unplacedIds.forEach((id, index) => {
					positionsRef.current[id] = spreadAround(dropPoint, index)
				})
			}

			// Physics is only there to place the nodes we have no position for. With none of
			// those left it stays off, which keeps a graph that is opened again exactly as it
			// was left instead of letting the solver rearrange it. An arrangement the user
			// asked for is theirs to end, so it is left running.
			if (unplacedIds.length > 0) setPhysics(true)
			else if (!manualLayoutRef.current) setPhysics(false)

			const positions: NodePositions = {}
			instances.forEach(({ id }) => {
				const position = id ? positionsRef.current[id] : undefined
				if (id && position) positions[id] = position
			})

			return positions
		},
		[useCaseId, syncFromNetwork, takeDropPoint, setPhysics],
	)

	useEffect(() => {
		if (!visNetwork || !useCaseId) return

		// The layout has come to rest, so there is nothing left for the solver to do. vis-network
		// emits this from a timeout of its own, so switching physics off here does not re-enter
		// the engine that reports it.
		const handleStabilized = () => {
			manualLayoutRef.current = false
			setIsLayoutRunning(false)
			setPhysics(false)
			schedulePersist()
		}

		visNetwork.on('stabilized', handleStabilized)
		visNetwork.on('dragEnd', schedulePersist)

		return () => {
			visNetwork.off('stabilized', handleStabilized)
			visNetwork.off('dragEnd', schedulePersist)

			// Flush a pending write, otherwise the last layout is lost.
			if (saveTimerRef.current !== null) {
				window.clearTimeout(saveTimerRef.current)
				saveTimerRef.current = null
				persist()
			}
		}
	}, [visNetwork, useCaseId, setPhysics, persist, schedulePersist])

	return { resolveNodePositions, rememberCreatePosition, startLayout, stopLayout, isLayoutRunning }
}
