// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { ReactNode, useDeferredValue, useMemo } from 'react'
import { useMaterialReactTable, type MRT_ColumnDef } from 'material-react-table'
import {
	Guideline_Model_Enums_StorageType as StorageType,
	InstanceService_Api_Dto_Instance as Instance,
	InstanceService_Models_Enum_Accessibility as Accessibility,
	InstanceService_Models_Enum_PropertyRight as PropertyRight,
} from '../../../services/instance'
import LockOutlineIcon from '@mui/icons-material/LockOutlined'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import { Box, Checkbox, Chip, IconButton, Tooltip } from '@mui/material'
import { useGetInstances, useGetClassification } from '../../../hooks'
import {
	accessibilityDotClasses,
	bufferEditOnBlur,
	bufferEditOnChange,
	propertySetHeaderProps,
	tableDefaults,
} from './tableSetup'
import { pendingCellSx } from '../../instances/pendingSx'
import { INSTANCE_NAME_FIELD, type InstanceEdits } from '../../instances/useInstanceEdits'
import { CellValue } from './components/CellValue'
import { BulkDeleteButton } from './components/BulkDeleteButton'

/** Column key of the classification, which is not a guideline property. */
const INSTANCE_CLASS = 'InstanceClass'

/** One row: the instance id, the two static columns and one entry per property. */
type TableRow = { id: string } & Record<string, string>

interface PropertyColumn {
	name: string
	propertySetName: string
	isWritable: boolean
	isBoolean: boolean
	inputType: string
	enumValues: string[]
}

interface InstanceTableProps {
	useCaseId?: string
	data: Instance[]
	/** The buffer both table views share, so edits survive a change of the filter. */
	edits: InstanceEdits
	/** Opens the edit dialog of an instance. */
	onOpenInstance: (instanceId: string) => void
	/** Rendered on the left of the table toolbar. */
	renderToolbarActions?: () => ReactNode
}

/** The HTML input type a property is edited with. */
const getInputType = (storageType?: StorageType | null): string => {
	switch (storageType) {
		case StorageType.Integer:
		case StorageType.Real:
			return 'number'
		case StorageType.Date:
			return 'date'
		case StorageType.Time:
			return 'time'
		default:
			return 'text'
	}
}

/**
 * Lists the instances of one classification with their properties, grouped by property
 * set as in the instance dialog. Cells are edited in place, see useInstanceEdits.
 */
export const useInstancePropertiesTable = ({
	useCaseId,
	data,
	edits: liveEdits,
	onOpenInstance,
	renderToolbarActions,
}: InstanceTableProps) => {
	// Read at low priority: every keystroke in the edit dialog changes the buffer, and the
	// rows and columns below are built from it. Writing is unaffected by a snapshot that
	// lags behind, because the setters of the buffer are functional updates.
	const edits = useDeferredValue(liveEdits)

	const classification = useMemo(
		() => encodeURIComponent(data[0]?.classificationId ?? ''),
		[data],
	)

	const { data: nodesMetaData } = useGetInstances(
		useCaseId,
		data.map(instance => instance.id!),
	)

	/** Defines which property columns exist and who may write them. */
	const { data: classificationData } = useGetClassification(useCaseId, classification)

	/** Accessibility per instance, which the metadata itself does not carry. */
	const accessibilityById = useMemo(() => {
		const map = new Map<string, Instance['accessibility']>()
		data.forEach(instance => instance.id && map.set(instance.id, instance.accessibility))
		return map
	}, [data])

	/** Enum options come from the instance metadata, which resolves them per property. */
	const enumValuesByProperty = useMemo<Record<string, string[]>>(() => {
		const map: Record<string, string[]> = {}
		nodesMetaData?.forEach(metadata =>
			metadata.properties?.forEach(property => {
				if (property.name && !map[property.name] && property.enumValues?.length) {
					map[property.name] = property.enumValues.map(value => value.name ?? '')
				}
			}),
		)
		return map
	}, [nodesMetaData])

	/** In guideline order; properties the user may not read at all are left out. */
	const propertyColumns = useMemo<PropertyColumn[]>(
		() =>
			classificationData?.propertySets?.flatMap(propertySet =>
				(propertySet.properties ?? [])
					.filter(property => property.right !== PropertyRight.None && !!property.name)
					.map(property => {
						const storageType = property.storageType as unknown as StorageType
						return {
							name: property.name!,
							propertySetName: propertySet.name ?? 'General',
							isWritable: property.right === PropertyRight.Write,
							isBoolean: storageType === StorageType.Boolean,
							inputType: getInputType(storageType),
							enumValues: enumValuesByProperty[property.name!] ?? [],
						}
					}),
			) ?? [],
		[classificationData, enumValuesByProperty],
	)

	/** Flat rows for the table: metadata with the buffered edits applied. */
	const rows = useMemo<TableRow[]>(
		() =>
			nodesMetaData?.map(metadata => {
				const id = metadata.id!
				const properties = Object.fromEntries(
					propertyColumns.map(column => [
						column.name,
						edits.getEdit(id, column.name)
							?? metadata.properties?.find(property => property.name === column.name)?.value
							?? '',
					]),
				)

				return {
					id,
					[INSTANCE_NAME_FIELD]: edits.getEdit(id, INSTANCE_NAME_FIELD) ?? metadata.name ?? '',
					[INSTANCE_CLASS]: metadata.classificationName ?? '',
					...properties,
				}
			}) ?? [],
		[nodesMetaData, propertyColumns, edits],
	)

	const columns = useMemo<MRT_ColumnDef<TableRow>[]>(() => {
		const propertySets = propertyColumns.reduce<Record<string, PropertyColumn[]>>((sets, column) => {
			sets[column.propertySetName] = [...(sets[column.propertySetName] ?? []), column]
			return sets
		}, {})

		const propertySetColumns: MRT_ColumnDef<TableRow>[] = Object.entries(propertySets).map(
			([propertySetName, propertiesOfSet]) => ({
				header: propertySetName,
				muiTableHeadCellProps: propertySetHeaderProps,
				columns: propertiesOfSet.map(column => ({
					accessorKey: column.name,
					header: column.name,
					enableEditing: column.isWritable && !column.isBoolean,
					...(column.enumValues.length > 0 && {
						editVariant: 'select' as const,
						editSelectOptions: column.enumValues,
					}),
					Header: () => (
						<div className="flex items-baseline gap-1">
							{!column.isWritable && (
								<Tooltip title="Property is readonly" placement="top">
									<LockOutlineIcon fontSize="small" />
								</Tooltip>
							)}
							{column.name}
						</div>
					),
					Cell: ({ row }) => {
						const isPending = edits.getEdit(row.original.id, column.name) !== undefined

						// Booleans toggle on a single click; there is nothing to type.
						if (column.isBoolean) {
							return (
								<Checkbox
									checked={row.original[column.name] === 'true'}
									disabled={!column.isWritable}
									size="small"
									sx={isPending ? pendingCellSx : undefined}
									onChange={event =>
										edits.setEdit(row.original.id, column.name, String(event.target.checked))
									}
								/>
							)
						}

						return (
							<CellValue
								value={row.original[column.name]}
								isPending={isPending}
								isReadOnly={!column.isWritable}
							/>
						)
					},
					muiEditTextFieldProps: ({ row }) => ({
						...(column.enumValues.length > 0
							? { onChange: bufferEditOnChange(edits, row.original.id, column.name) }
							: { type: column.inputType }),
						...(column.inputType === 'date' || column.inputType === 'time'
							? { InputLabelProps: { shrink: true } }
							: {}),
						onBlur: bufferEditOnBlur(edits, row.original.id, column.name, row.original[column.name]),
					}),
				})),
			}),
		)

		return [
			{
				accessorKey: INSTANCE_NAME_FIELD,
				header: INSTANCE_NAME_FIELD,
				enableEditing: row => accessibilityById.get(row.original.id) !== Accessibility.ReadOnly,
				Cell: ({ row }) => {
					const accessibility = accessibilityById.get(row.original.id)

					return (
						<div className="flex items-center gap-1">
							<span className={accessibilityDotClasses(accessibility)} />
							<CellValue
								value={row.original[INSTANCE_NAME_FIELD]}
								isPending={edits.getEdit(row.original.id, INSTANCE_NAME_FIELD) !== undefined}
								isReadOnly={accessibility === Accessibility.ReadOnly}
							/>
						</div>
					)
				},
				muiEditTextFieldProps: ({ row }) => ({
					required: true,
					onBlur: bufferEditOnBlur(
						edits,
						row.original.id,
						INSTANCE_NAME_FIELD,
						row.original[INSTANCE_NAME_FIELD],
					),
				}),
			},
			{
				accessorKey: INSTANCE_CLASS,
				header: 'Instance Class',
				enableEditing: false,
				Cell: ({ row }) => (
					<Chip label={row.original[INSTANCE_CLASS]} size="small" variant="outlined" />
				),
			},
			...propertySetColumns,
		]
	}, [propertyColumns, accessibilityById, edits])

	return useMaterialReactTable({
		...tableDefaults,
		columns,
		data: rows,
		getRowId: row => row.id,
		enableGlobalFilter: true,
		renderTopToolbarCustomActions: ({ table }) => (
			<Box className="flex flex-wrap items-center gap-2">
				{renderToolbarActions?.()}
				<BulkDeleteButton
					useCaseId={useCaseId}
					instances={table.getSelectedRowModel().rows.map(row => ({
						id: row.original.id,
						name: row.original[INSTANCE_NAME_FIELD],
					}))}
					onDeleted={deletedIds => {
						deletedIds.forEach(id => edits.discardInstance(id))
						table.resetRowSelection()
					}}
				/>
			</Box>
		),
		// No row click handler on purpose: a click has to focus the cell, like in a
		// spreadsheet. The action below is the way into the dialog.
		renderRowActions: ({ row }) => (
			<Tooltip title="Open instance">
				<IconButton size="small" onClick={() => onOpenInstance(row.original.id)}>
					<EditOutlinedIcon fontSize="small" />
				</IconButton>
			</Tooltip>
		),
	})
}
