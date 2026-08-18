// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { ToggleButton, ToggleButtonGroup } from '@mui/material'
import PolylineIcon from '@mui/icons-material/Polyline'
import TableChartIcon from '@mui/icons-material/TableChart'
import { createSearchParams, useNavigate } from 'react-router-dom'
import { Route } from '../../routes/instancesRoute'

interface ViewSwitchProps {
	isNodeView: boolean
	setIsNodeView: (isNodeView: boolean) => void
}

/**
 * Switches between graph and table. Both views show the same instances of the use case,
 * so switching clears what is specific to one of them: the opened instance and the filters.
 */
export const ViewSwitch = ({ isNodeView, setIsNodeView }: Readonly<ViewSwitchProps>) => {
	const navigate = useNavigate()
	const { useCaseId, textQuery } = Route.useSearch()

	const selectView = (nodeView: boolean) => {
		setIsNodeView(nodeView)

		if (!useCaseId) return

		const searchParams = {
			useCaseId: `${useCaseId}`,
			...(textQuery && { textQuery: `${textQuery}` }),
		}

		navigate({
			pathname: Route.path,
			search: `?${createSearchParams(searchParams).toString()}`,
		})
	}

	return (
		<ToggleButtonGroup
			exclusive
			size="small"
			color="secondary"
			value={isNodeView ? 'graph' : 'table'}
			onChange={(_event, value) => value !== null && selectView(value === 'graph')}
		>
			<ToggleButton value="graph" sx={{ gap: 0.75, px: 1.5 }}>
				<PolylineIcon fontSize="small" />
				Graph
			</ToggleButton>
			<ToggleButton value="table" sx={{ gap: 0.75, px: 1.5 }}>
				<TableChartIcon fontSize="small" />
				Table
			</ToggleButton>
		</ToggleButtonGroup>
	)
}
