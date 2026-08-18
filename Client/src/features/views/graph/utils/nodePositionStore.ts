// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/**
 * Persists graph node positions in the browser, keyed per use case, so that a reload
 * shows the settled layout instead of running the force simulation again.
 */

export interface NodePosition {
	x: number
	y: number
}

export type NodePositions = Record<string, NodePosition>

const STORAGE_KEY_PREFIX = 'gaeco.instanceGraph.positions.v1'

/** Upper bound on stored entries, so positions of deleted instances cannot pile up. */
const MAX_STORED_POSITIONS = 10000

const storageKey = (useCaseId: string): string => `${STORAGE_KEY_PREFIX}.${useCaseId}`

const isValidPosition = (value: unknown): value is NodePosition => {
	const position = value as Partial<NodePosition> | null
	return !!position && Number.isFinite(position.x) && Number.isFinite(position.y)
}

/**
 * Keeps the most recently written entries once the cap is exceeded.
 */
const capPositions = (positions: NodePositions): NodePositions => {
	const ids = Object.keys(positions)
	if (ids.length <= MAX_STORED_POSITIONS) return positions

	const capped: NodePositions = {}
	ids.slice(ids.length - MAX_STORED_POSITIONS).forEach(id => {
		capped[id] = positions[id]
	})
	return capped
}

/**
 * Reads the stored positions of a use case. Returns an empty map when nothing is
 * stored, the entry is corrupt or storage is unavailable (private mode, quota).
 */
export const loadNodePositions = (useCaseId?: string | null): NodePositions => {
	if (!useCaseId) return {}

	try {
		const raw = window.localStorage.getItem(storageKey(useCaseId))
		if (!raw) return {}

		const parsed: unknown = JSON.parse(raw)
		if (!parsed || typeof parsed !== 'object') return {}

		const positions: NodePositions = {}
		Object.entries(parsed as Record<string, unknown>).forEach(([id, value]) => {
			if (isValidPosition(value)) positions[id] = { x: value.x, y: value.y }
		})
		return positions
	} catch {
		return {}
	}
}

/**
 * Writes the positions of a use case. Failures are ignored: the stored layout is a
 * convenience and must never break the graph view.
 */
export const saveNodePositions = (
	useCaseId: string | null | undefined,
	positions: NodePositions,
): void => {
	if (!useCaseId) return

	try {
		window.localStorage.setItem(storageKey(useCaseId), JSON.stringify(capPositions(positions)))
	} catch {
		/* storage full or unavailable */
	}
}
