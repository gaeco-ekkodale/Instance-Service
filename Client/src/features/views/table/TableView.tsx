// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box, Button, IconButton, Paper, Tooltip } from '@mui/material'
import AddIcon from '@mui/icons-material/Add'
import FileDownloadIcon from '@mui/icons-material/FileDownload'
import { MaterialReactTable } from 'material-react-table'
import { useState } from 'react'
import { useFormattedTimestamp } from '../../../hooks/useFormattedTimestamp'
import { useInstances } from '../../instances/instancesContext'
import TableSearchbar from '../../shell/TableSearchbar'
import { downloadCsv, tableToCsv } from './csvExport'
import { TableInstanceDialogs } from './TableInstanceDialogs'
import { useInstancePropertiesTable } from './useInstancePropertiesTable'
import { useInstanceTable } from './useInstanceTable'

/**
 * The same instances as a spreadsheet. Which of the two tables shows depends on whether a
 * classification is picked: only then do its properties exist as columns.
 */
export const TableView = () => {
	const {
		isNodeView,
		useCaseId,
		classificationId,
		instanceData,
		instanceDataProperties,
		navigateToNode,
		edits,
	} = useInstances()
	const { getFormattedTimestamp } = useFormattedTimestamp()
	const [createOpen, setCreateOpen] = useState(false)

	// An opened instance would turn the create dialog into the "create with relation" flow.
	const startCreate = () => {
		navigateToNode(undefined)
		setCreateOpen(true)
	}

	/** Walks every visible row, so the file is built on demand and not on each render. */
	const exportCsv = () => {
		const csv = classificationId ? tableToCsv(propertiesTable) : tableToCsv(table)
		downloadCsv(csv, `table-dump_${getFormattedTimestamp()}.csv`)
	}

	// A function, not a node: it reads the table that is showing.
	const renderToolbarActions = () => (
		<Box className="flex items-center gap-2">
			<Button variant="contained" color="secondary" startIcon={<AddIcon />} onClick={startCreate}>
				New
			</Button>
			<TableSearchbar instances={instanceData} />
			<Tooltip title="Download CSV">
				<IconButton size="small" onClick={exportCsv}>
					<FileDownloadIcon className="text-[rgba(0,0,0,0.6)]" />
				</IconButton>
			</Tooltip>
		</Box>
	)

	const table = useInstanceTable({
		data: instanceData,
		useCaseId: useCaseId ?? '',
		edits,
		onOpenInstance: navigateToNode,
		renderToolbarActions,
	})

	const propertiesTable = useInstancePropertiesTable({
		data: instanceDataProperties,
		useCaseId: useCaseId ?? '',
		edits,
		onOpenInstance: navigateToNode,
		renderToolbarActions,
	})

	return (
		<>
			<Paper elevation={2} className="flex h-full flex-col overflow-hidden rounded-md">
				{classificationId ? (
					<MaterialReactTable key="properties-table" table={propertiesTable} />
				) : (
					<MaterialReactTable key="instance-table" table={table} />
				)}
			</Paper>
			{/* Only while showing: the dialogs render into a portal, which hiding the container
			    would not reach, and their focus trap would fight the graph's. */}
			{!isNodeView && (
				<TableInstanceDialogs createOpen={createOpen} onCloseCreate={() => setCreateOpen(false)} />
			)}
		</>
	)
}
