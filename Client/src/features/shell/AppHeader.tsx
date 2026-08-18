// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import SearchIcon from '@mui/icons-material/Search'
import TerminalIcon from '@mui/icons-material/Terminal'
import CheckIcon from '@mui/icons-material/Check'
import { Box, Divider, IconButton, Paper, Tooltip, Typography } from '@mui/material'
import { useState } from 'react'
import Searchbar from './Searchbar'
import UseCaseSelector from './UseCaseSelector'
import QueryDialog from './QueryDialog'
import { ViewSwitch } from './ViewSwitch'
import { PendingChanges } from './PendingChanges'
import { useInstances } from '../instances/instancesContext'

/**
 * Header of the module: use case, the Cypher query, the unsaved changes and the
 * graph/table switch. It sits in the page flow, so nothing of it overlaps the view below.
 */
export default function AppHeader() {
	const { textQuery, isNodeView, setIsNodeView } = useInstances()
	const hasActiveQuery = !!textQuery

	const [searchOpen, setSearchOpen] = useState<boolean>(false)
	const [queryOpen, setQueryOpen] = useState<boolean>(false)

	return (
		<Paper square elevation={2} className="z-30 flex h-16 shrink-0 items-center gap-3 px-3">
			<Typography variant="h6" noWrap className="title-app">
				Instance Viewer
			</Typography>

			<Divider orientation="vertical" flexItem className="my-3" />

			<Box className="min-w-52">
				<UseCaseSelector />
			</Box>

			{/* Finding a single node only makes sense on the canvas; the table filters itself. */}
			{isNodeView && (
				<>
					<Tooltip title="Find instance">
						<IconButton
							onClick={() => setSearchOpen(!searchOpen)}
							color="inherit"
							aria-label="find instance"
							className="icon_Search"
						>
							<SearchIcon />
						</IconButton>
					</Tooltip>
					{searchOpen && <Searchbar />}
				</>
			)}

			<Tooltip title={hasActiveQuery ? 'Edit active query' : 'Create Neo4J Cypher query'}>
				<IconButton onClick={() => setQueryOpen(true)} color="inherit" aria-label="cypher query">
					<TerminalIcon />
					{hasActiveQuery && <CheckIcon fontSize="small" />}
				</IconButton>
			</Tooltip>

			{/* Grows into the free space, so the switch stays on the right. */}
			<Box className="flex-1" />

			{/* Buffered edits belong to the page, so they are saved here and stay
			    reachable from both views. */}
			<PendingChanges />

			<ViewSwitch isNodeView={isNodeView} setIsNodeView={setIsNodeView} />

			<QueryDialog open={queryOpen} onClose={() => setQueryOpen(false)} />
		</Paper>
	)
}
