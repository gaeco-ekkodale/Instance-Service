// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useMemo, useState } from 'react'
import { QueryClient, useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import { InstancesService } from '../../services/instance'

/** Field key that carries the instance name instead of a guideline property. */
export const INSTANCE_NAME_FIELD = 'InstanceName'

/** Buffered values per instance id and field. */
type PendingEdits = Record<string, Record<string, string>>

/** Queries holding instance values, to be refreshed after a write. */
export const INSTANCE_QUERY_KEYS = ['nodesGraph', 'filteredNodesGraph', 'nodesMetaData', 'nodeMetaData']

/** Stands in for an instance without buffered fields, stable enough to be a dependency. */
const NO_EDITS: Record<string, string> = {}

/**
 * The buffer holds strings, while the property editors of the dialog hand over numbers as
 * well. A cleared number field yields NaN, which must not reach a cell as "NaN".
 */
export const normalizeEditValue = (value: unknown): string => {
	if (value === undefined || value === null) return ''
	if (typeof value === 'number') return Number.isFinite(value) ? String(value) : ''
	return String(value)
}

export interface InstanceEdits {
	/** The buffered value of a field, or undefined when it was not edited. */
	getEdit: (instanceId: string, field: string) => string | undefined
	/** Buffers a value; nothing is sent to the server yet. */
	setEdit: (instanceId: string, field: string, value: unknown) => void
	/** Drops one buffered field, for a value that was edited back to the stored one. */
	clearEdit: (instanceId: string, field: string) => void
	/** Every buffered field of one instance, empty while it was not edited. */
	getInstanceEdits: (instanceId: string) => Record<string, string>
	/** Drops every buffered field of one instance. */
	discardInstance: (instanceId: string) => void
	/** Ids of the instances holding buffered fields, to mark them in the views. */
	editedInstanceIds: Set<string>
	/** Number of buffered field values. */
	pendingCount: number
	isSaving: boolean
	/** Sends one request per edited instance. */
	save: () => Promise<void>
	discard: () => void
}

const withoutInstance = (current: PendingEdits, instanceId: string): PendingEdits => {
	if (!current[instanceId]) return current

	const next = { ...current }
	delete next[instanceId]
	return next
}

/** Drops one field, and with its last field the instance as well. */
const withoutField = (current: PendingEdits, instanceId: string, field: string): PendingEdits => {
	const fields = current[instanceId]
	if (!fields || fields[field] === undefined) return current

	const remaining = Object.entries(fields).filter(([name]) => name !== field)
	if (remaining.length === 0) return withoutInstance(current, instanceId)

	return { ...current, [instanceId]: Object.fromEntries(remaining) }
}

/**
 * The API replaces name and properties as a whole, so the current metadata is read and
 * sent back with the buffered values applied.
 */
const saveInstance = async (
	queryClient: QueryClient,
	useCaseId: string,
	instanceId: string,
	changes: Record<string, string>,
) => {
	const metadata = await queryClient.fetchQuery({
		queryKey: ['nodeMetaData', instanceId],
		queryFn: () => InstancesService.getInstance(instanceId, useCaseId),
	})

	const properties: Record<string, string> = {}
	metadata.properties?.forEach(property => {
		if (property.name) properties[property.name] = property.value ?? ''
	})

	let name = metadata.name ?? ''
	Object.entries(changes).forEach(([field, value]) => {
		if (field === INSTANCE_NAME_FIELD) name = value
		else properties[field] = value
	})

	await InstancesService.updateInstance(useCaseId, instanceId, { name, properties })
}

/**
 * Buffers instance edits of the whole page until they are saved together. Both tables and
 * the edit dialog write into it, so there is a single place that talks to the server and a
 * single set of unsaved values, whichever view or editor produced them.
 */
export const useInstanceEdits = (useCaseId?: string | null): InstanceEdits => {
	const queryClient = useQueryClient()
	const [pendingEdits, setPendingEdits] = useState<PendingEdits>({})
	const [isSaving, setIsSaving] = useState<boolean>(false)
	const [editedUseCaseId, setEditedUseCaseId] = useState(useCaseId)

	// Instances of another use case are out of reach and must not be written into this one.
	// Reset during the render, so no view ever paints the buffer of the use case left behind.
	if (useCaseId !== editedUseCaseId) {
		setEditedUseCaseId(useCaseId)
		setPendingEdits({})
	}

	// Kept apart from the buffer below: the graph marks whole instances, so it must not rebuild
	// its nodes for every field that is typed into one already marked. Instance ids are GUIDs,
	// so a comma separates them without ever occurring inside one.
	const idsKey = Object.keys(pendingEdits).sort().join(',')
	const editedInstanceIds = useMemo(() => new Set(idsKey ? idsKey.split(',') : []), [idsKey])

	// One object per state of the buffer, so the views rebuild exactly when an edit lands.
	return useMemo(() => {
		const save = async () => {
			if (!useCaseId) {
				toast.error('No UseCase found, make sure to select one first.')
				return
			}

			const editedInstances = Object.entries(pendingEdits)
			if (editedInstances.length === 0) return

			setIsSaving(true)

			// What could not be saved stays in the buffer, so nothing typed is lost.
			const failedEdits: PendingEdits = {}

			for (const [instanceId, changes] of editedInstances) {
				try {
					await saveInstance(queryClient, useCaseId, instanceId, changes)
				} catch {
					failedEdits[instanceId] = changes
				}
			}

			setPendingEdits(failedEdits)
			setIsSaving(false)
			INSTANCE_QUERY_KEYS.forEach(key => queryClient.invalidateQueries({ queryKey: [key] }))

			const failed = Object.keys(failedEdits).length
			if (failed > 0) {
				toast.error(`${failed} of ${editedInstances.length} instances could not be saved`)
			} else {
				toast.success(
					editedInstances.length === 1
						? 'Instance saved'
						: `${editedInstances.length} instances saved`,
				)
			}
		}

		return {
			getEdit: (instanceId, field) => pendingEdits[instanceId]?.[field],
			setEdit: (instanceId, field, value) =>
				setPendingEdits(current => ({
					...current,
					[instanceId]: { ...current[instanceId], [field]: normalizeEditValue(value) },
				})),
			clearEdit: (instanceId, field) =>
				setPendingEdits(current => withoutField(current, instanceId, field)),
			getInstanceEdits: instanceId => pendingEdits[instanceId] ?? NO_EDITS,
			discardInstance: instanceId =>
				setPendingEdits(current => withoutInstance(current, instanceId)),
			editedInstanceIds,
			pendingCount: Object.values(pendingEdits).reduce(
				(count, fields) => count + Object.keys(fields).length,
				0,
			),
			isSaving,
			save,
			discard: () => setPendingEdits({}),
		}
	}, [pendingEdits, editedInstanceIds, isSaving, useCaseId, queryClient])
}
