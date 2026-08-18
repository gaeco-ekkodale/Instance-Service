// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Fragment, useMemo, useState } from 'react'
import {
	Box,
	Button,
	Chip,
	IconButton,
	List,
	ListItem,
	ListSubheader,
	Popover,
	Tooltip,
} from '@mui/material'
import SaveOutlinedIcon from '@mui/icons-material/SaveOutlined'
import UndoOutlinedIcon from '@mui/icons-material/UndoOutlined'
import { useInstances } from '../instances/instancesContext'
import { INSTANCE_NAME_FIELD } from '../instances/useInstanceEdits'

/**
 * Shows how many instance edits are buffered and lets them be saved or dropped. The chip
 * opens a list of them, because an edit of the dialog has no column in the unfiltered
 * table and would otherwise only show up as a number.
 * Renders nothing while there is nothing to save.
 */
export const PendingChanges = () => {
	const { instanceData: instances, edits } = useInstances()
	const { pendingCount, isSaving, save, discard, editedInstanceIds, getEdit, getInstanceEdits, discardInstance } =
		edits
	const [detailsAnchor, setDetailsAnchor] = useState<HTMLElement | null>(null)

	const nameById = useMemo(
		() => new Map(instances.filter(instance => instance.id).map(instance => [instance.id!, instance.name])),
		[instances],
	)

	if (pendingCount === 0) return null

	const instanceName = (instanceId: string) =>
		getEdit(instanceId, INSTANCE_NAME_FIELD) || nameById.get(instanceId) || instanceId

	return (
		<Box className="flex shrink-0 items-center gap-1">
			<Tooltip title="Show the buffered changes">
				<Chip
					size="small"
					color="warning"
					clickable
					onClick={event => setDetailsAnchor(event.currentTarget)}
					label={pendingCount === 1 ? '1 unsaved change' : `${pendingCount} unsaved changes`}
				/>
			</Tooltip>
			<Button
				size="small"
				variant="contained"
				color="secondary"
				startIcon={<SaveOutlinedIcon />}
				disabled={isSaving}
				onClick={save}
			>
				Save
			</Button>
			<Button
				size="small"
				variant="outlined"
				startIcon={<UndoOutlinedIcon />}
				disabled={isSaving}
				onClick={discard}
			>
				Discard
			</Button>

			<Popover
				open={!!detailsAnchor}
				anchorEl={detailsAnchor}
				onClose={() => setDetailsAnchor(null)}
				anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
			>
				{/* One line per field, the instance names sticky while scrolling through many. */}
				<List dense disablePadding className="max-h-96 w-96 overflow-auto">
					{[...editedInstanceIds].map(instanceId => (
						<Fragment key={instanceId}>
							<ListSubheader className="flex items-center justify-between gap-2">
								<span className="truncate">{instanceName(instanceId)}</span>
								<Tooltip title="Revert this instance">
									<IconButton size="small" onClick={() => discardInstance(instanceId)}>
										<UndoOutlinedIcon fontSize="small" />
									</IconButton>
								</Tooltip>
							</ListSubheader>
							{Object.entries(getInstanceEdits(instanceId)).map(([field, value]) => (
								<ListItem key={field} className="flex justify-between gap-4 text-xs">
									<span className="min-w-0 truncate text-gray-500">{field}</span>
									<span className="min-w-0 truncate">{value || '—'}</span>
								</ListItem>
							))}
						</Fragment>
					))}
				</List>
			</Popover>
		</Box>
	)
}
