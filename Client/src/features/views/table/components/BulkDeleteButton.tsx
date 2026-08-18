// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { toast } from 'sonner'
import {
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogContentText,
	DialogTitle,
} from '@mui/material'
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline'
import { InstancesService } from '../../../../services/instance'
import { INSTANCE_QUERY_KEYS } from '../../../instances/useInstanceEdits'

/** How many names the confirmation lists before summarising the rest. */
const MAX_LISTED_NAMES = 5

export interface DeletableInstance {
	id: string
	name: string
}

interface BulkDeleteButtonProps {
	useCaseId?: string | null
	/** The rows currently selected in the table. */
	instances: DeletableInstance[]
	/**
	 * Called after the deletions ran with the ids that are actually gone, to clear the
	 * selection and to drop whatever was still buffered for them.
	 */
	onDeleted: (deletedIds: string[]) => void
}

/**
 * Deletes the selected instances after a confirmation naming them.
 * Instances holding read-only data are rejected by the server and reported as failures.
 */
export const BulkDeleteButton = ({
	useCaseId,
	instances,
	onDeleted,
}: Readonly<BulkDeleteButtonProps>) => {
	const queryClient = useQueryClient()
	const [confirmOpen, setConfirmOpen] = useState<boolean>(false)
	const [isDeleting, setIsDeleting] = useState<boolean>(false)

	if (instances.length === 0) return null

	const listedNames = instances.slice(0, MAX_LISTED_NAMES).map(instance => instance.name || instance.id)
	const remaining = instances.length - listedNames.length

	const handleDelete = async () => {
		setConfirmOpen(false)
		setIsDeleting(true)

		const deletedIds: string[] = []
		for (const instance of instances) {
			try {
				await InstancesService.deleteInstance(useCaseId!, instance.id)
				deletedIds.push(instance.id)
			} catch {
				// Counted below; the instance stays, so its buffered edits stay too.
			}
		}

		const failed = instances.length - deletedIds.length

		setIsDeleting(false)
		onDeleted(deletedIds)
		INSTANCE_QUERY_KEYS.forEach(key => queryClient.invalidateQueries({ queryKey: [key] }))

		if (failed > 0) {
			toast.error(
				`${failed} of ${instances.length} instances could not be deleted - they hold read-only data or you lack the rights`,
			)
		} else {
			toast.success(instances.length === 1 ? 'Instance deleted' : `${instances.length} instances deleted`)
		}
	}

	return (
		<>
			<Button
				size="small"
				variant="contained"
				color="error"
				startIcon={<DeleteOutlineIcon />}
				disabled={isDeleting || !useCaseId}
				onClick={() => setConfirmOpen(true)}
			>
				{`Delete (${instances.length})`}
			</Button>

			<Dialog open={confirmOpen} onClose={() => setConfirmOpen(false)}>
				<DialogTitle>
					{instances.length === 1 ? 'Delete instance?' : `Delete ${instances.length} instances?`}
				</DialogTitle>
				<DialogContent>
					<DialogContentText>
						{listedNames.join(', ')}
						{remaining > 0 && ` and ${remaining} more`}
						{' — these and all their connections are removed permanently.'}
					</DialogContentText>
				</DialogContent>
				<DialogActions>
					<Button onClick={() => setConfirmOpen(false)}>Cancel</Button>
					<Button variant="contained" color="error" onClick={handleDelete}>
						Delete
					</Button>
				</DialogActions>
			</Dialog>
		</>
	)
}
