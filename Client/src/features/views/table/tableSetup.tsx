// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { ChangeEvent, FocusEvent, KeyboardEvent } from 'react';
import { Theme } from '@mui/material';
import {
	isCellEditable,
	openEditingCell,
	type MRT_Cell,
	type MRT_RowData,
	type MRT_TableInstance,
} from 'material-react-table';
import { InstanceService_Models_Enum_Accessibility as Accessibility } from '../../../services/instance';
import { InstanceEdits } from '../../instances/useInstanceEdits';

/** Hover hint of an editable cell. Inlined, so the grid needs no request to draw itself. */
const PENCIL_ICON =
	"url(\"data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' width='11' height='11' viewBox='0 0 24 24' fill='%239e9e9e'%3e%3cpath d='M3 17.25V21h3.75L17.81 9.94l-3.75-3.75L3 17.25zM20.71 7.04a1 1 0 0 0 0-1.41l-2.34-2.34a1 1 0 0 0-1.41 0l-1.83 1.83 3.75 3.75 1.83-1.83z'/%3e%3c/svg%3e\")";

/**
 * Shared Material React Table configuration of the instance tables.
 *
 * Editing follows the spreadsheet convention material-react-table implements: a single
 * click focuses a cell, a double click opens its editor, Ctrl+C copies. Values are
 * buffered and saved together, see useInstanceEdits. The table fills the height of its
 * container, so the toolbars and the header stay put while the rows scroll.
 */
export const tableDefaults = {
	enableEditing: true,
	editDisplayMode: 'cell' as const,
	enableRowSelection: true,
	enableRowActions: true,
	positionActionsColumn: 'last' as const,
	// The "n of m rows selected" banner belongs to the pagination, not above the table.
	positionToolbarAlertBanner: 'bottom' as const,
	enableStickyHeader: true,
	enableColumnPinning: true,
	initialState: {
		density: 'compact' as const,
		columnPinning: { right: ['mrt-row-actions'] },
	},
	muiTablePaperProps: {
		elevation: 0,
		sx: { display: 'flex', flexDirection: 'column', height: '100%' },
	},
	// Scrolls in both directions, so many property columns stay reachable. The height comes
	// from the surrounding card, which is why the maxHeight material-react-table derives
	// from the viewport is dropped, and minHeight lets the flex item shrink at all.
	//
	// The grid and the editing affordance sit here rather than on the cells: one rule set for
	// the whole table instead of an sx object built per cell on every render, and column-level
	// props cannot drop the borders by overriding them.
	muiTableContainerProps: {
		sx: (theme: Theme) => ({
			flex: 1,
			minHeight: 0,
			maxHeight: 'none',
			overflow: 'auto',

			// Ruled like a spreadsheet, so a value is read against its own column and row.
			'& th, & td': {
				borderRight: `1px solid ${theme.palette.divider}`,
				borderBottom: `1px solid ${theme.palette.divider}`,
			},
			'& thead th': { backgroundColor: theme.palette.grey[100] },

			// What a double click reaches, shown as a pencil on hover. The attribute is set only
			// on cells that are actually editable right now. Drawn as a background rather than a
			// positioned pseudo element, which would have to claim the cell's `position` and with
			// it break a pinned column's stickiness.
			'& td[data-editable]:hover': {
				backgroundColor: theme.palette.action.hover,
				backgroundImage: PENCIL_ICON,
				backgroundRepeat: 'no-repeat',
				backgroundPosition: 'right 4px center',
			},

			'& td:focus': {
				outline: `2px solid ${theme.palette.secondary.main}`,
				outlineOffset: '-2px',
			},
		}),
	},
	// material-react-table anchors the pagination at right: 0, so it is moved inwards
	// directly - otherwise the floating help button of the viewport sits on top of it.
	muiBottomToolbarProps: { sx: { '& .MuiTablePagination-root': { mr: 7 } } },
	// The selection count is a note, not a warning - plain text instead of a blue banner.
	muiToolbarAlertBannerProps: {
		sx: { backgroundColor: 'transparent', color: 'text.secondary' },
	},
	muiTableBodyCellProps: <TRow extends MRT_RowData>({
		cell,
		table,
	}: {
		cell: MRT_Cell<TRow>
		table: MRT_TableInstance<TRow>
	}) => ({
		// Drives the hover tint and the pencil, both styled on the container above. Dropped
		// while the editor is open, where they would sit on top of the input.
		'data-editable':
			(table.getState().editingCell?.id !== cell.id && isCellEditable({ cell, table })) || undefined,
		// Enter and F2 open the editor of the focused cell, next to the double click.
		onKeyDown: (event: KeyboardEvent<HTMLTableCellElement>) => {
			if (event.key !== 'Enter' && event.key !== 'F2') return;
			event.preventDefault();
			openEditingCell({ cell, table });
		},
	}),
};

/**
 * Left-aligns a property set header over its first column instead of centering it
 * across the whole group.
 */
export const propertySetHeaderProps = {
	align: 'left' as const,
	sx: { '& .Mui-TableHeadCell-Content': { justifyContent: 'flex-start' } },
};

/**
 * Buffers the value of a closing cell editor. Enter closes it without moving the focus
 * anywhere, so the cell is focused again to keep the keyboard in the grid.
 *
 * shownValue is what the cell displays, so an untouched editor counts as no edit. It comes
 * from the row, not from the cell: material-react-table writes a picked select option into
 * the row value cache at once, which would make every selection look unchanged.
 */
export const bufferEditOnBlur = (
	edits: InstanceEdits,
	instanceId: string,
	field: string,
	shownValue: unknown,
) => (event: FocusEvent<HTMLInputElement | HTMLTextAreaElement>) => {
	// A MUI select swaps event.target for { value, name } when it blurs, while
	// currentTarget stays the display element, which carries no value at all. For text
	// inputs both are the same element, so the value is always read from the target.
	const value = event.target.value ?? '';
	if (value !== (shownValue ?? '')) edits.setEdit(instanceId, field, value);

	if (event.relatedTarget) return;

	const cellElement = event.currentTarget.closest('td');
	queueMicrotask(() => cellElement?.focus());
};

/**
 * Buffers a picked select option at once, like the boolean checkboxes do. Picking an option
 * is already the whole edit, and an open dropdown swallows the click that would otherwise
 * blur its editor.
 */
export const bufferEditOnChange = (
	edits: InstanceEdits,
	instanceId: string,
	field: string,
) => (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
	edits.setEdit(instanceId, field, event.target.value ?? '');
};

/**
 * Classes of the coloured dot in front of an instance name, showing what the current
 * user may do with the instance.
 */
export const accessibilityDotClasses = (accessibility?: Accessibility | null): string => {
	const base = 'w-3 h-3 rounded-full inline-block mr-2 shadow border-2';

	switch (accessibility) {
		case 'ReadOnly':
			return `${base} bg-red-300 border-red-400`;
		case 'FullControl':
			return `${base} bg-green-200 border-green-400`;
		case 'ReadWrite':
			return `${base} bg-yellow-100 border-yellow-400`;
		default:
			return `${base} bg-gray-400 border-gray-500`;
	}
};
