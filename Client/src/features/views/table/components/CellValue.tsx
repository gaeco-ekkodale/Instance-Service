// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box } from '@mui/material'
import { pendingCellSx } from '../../../instances/pendingSx'

interface CellValueProps {
	value?: string | null
	isPending?: boolean
	/** Greys out what the user may not change. */
	isReadOnly?: boolean
}

/**
 * The value of a table cell, marked when it carries a buffered edit.
 */
export const CellValue = ({ value, isPending, isReadOnly }: Readonly<CellValueProps>) => (
	<Box
		component="span"
		sx={isPending ? pendingCellSx : undefined}
		className={isReadOnly ? 'text-gray-400' : undefined}
	>
		{value}
	</Box>
)
