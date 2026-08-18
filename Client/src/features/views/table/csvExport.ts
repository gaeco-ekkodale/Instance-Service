// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import type { MRT_RowData, MRT_TableInstance } from 'material-react-table'

/**
 * Excel and Calc run a cell starting with = + - or @ as a formula, so those values are
 * prefixed with an apostrophe to stay text.
 */
const quote = (value: string) =>
	`"${(/^[=+\-@\t\r]/.test(value) ? `'${value}` : value).replace(/"/g, '""')}"`

/**
 * The visible rows and columns of a table, in the order they are shown. The display columns
 * of material-react-table hold a checkbox and a menu instead of a value and are left out.
 */
export const tableToCsv = <T extends MRT_RowData>(table: MRT_TableInstance<T>) => {
	const columns = table.getVisibleLeafColumns().filter(column => !column.id.startsWith('mrt-'))
	const header = columns.map(column => String(column.columnDef.header ?? ''))
	const body = table
		.getRowModel()
		.rows.map(row => columns.map(column => String(row.getValue(column.id) ?? '')))

	return [header, ...body].map(row => row.map(quote).join(',')).join('\n')
}

/** Hands a file to the browser without a server round trip. */
export const downloadCsv = (csv: string, filename: string) => {
	// The BOM is what makes Excel read the file as UTF-8 instead of the local codepage.
	const href = URL.createObjectURL(new Blob([`\uFEFF${csv}`], { type: 'text/csv;charset=utf-8' }))

	Object.assign(document.createElement('a'), { href, download: filename }).click()
	// Released late: revoking the URL right after the click can cancel the download.
	setTimeout(() => URL.revokeObjectURL(href), 1000)
}
