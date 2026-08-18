// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { ReactNode, useDeferredValue, useMemo } from 'react';
import { useMaterialReactTable, type MRT_ColumnDef } from 'material-react-table';
import {
    InstanceService_Models_Enum_Accessibility as Accessibility,
    InstanceService_Api_Dto_Instance as Instance,
} from '../../../services/instance';
import { Box, Chip, IconButton, Tooltip } from '@mui/material';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import { accessibilityDotClasses, bufferEditOnBlur, tableDefaults } from './tableSetup';
import { INSTANCE_NAME_FIELD, type InstanceEdits } from '../../instances/useInstanceEdits';
import { CellValue } from './components/CellValue';
import { BulkDeleteButton } from './components/BulkDeleteButton';

interface InstanceTableProps {
    useCaseId?: string;
    data: Instance[];
    /** The buffer both table views share, so edits survive a change of the filter. */
    edits: InstanceEdits;
    /** Opens the edit dialog of an instance. */
    onOpenInstance: (instanceId: string) => void;
    /** Rendered on the left of the table toolbar. */
    renderToolbarActions?: () => ReactNode;
}

/**
 * Lists the instances of the use case with their classification. The name is edited in
 * place, the properties in the dialog behind the row action.
 */
export const useInstanceTable = ({
    useCaseId,
    data,
    edits: liveEdits,
    onOpenInstance,
    renderToolbarActions,
}: InstanceTableProps) => {
    // Read at low priority: every keystroke in the edit dialog changes the buffer, and the
    // rows and columns below are built from it. Writing is unaffected by a snapshot that
    // lags behind, because the setters of the buffer are functional updates.
    const edits = useDeferredValue(liveEdits);

    /** The rows as they are shown: server values with the buffered edits applied. */
    const rows = useMemo<Instance[]>(
        () => data.map(instance => {
            const pendingName = instance.id ? edits.getEdit(instance.id, INSTANCE_NAME_FIELD) : undefined;
            return pendingName === undefined ? instance : { ...instance, name: pendingName };
        }),
        [data, edits],
    );

    const columns = useMemo<MRT_ColumnDef<Instance>[]>(() => [
        {
            accessorKey: 'name',
            header: INSTANCE_NAME_FIELD,
            enableEditing: row => row.original.accessibility !== Accessibility.ReadOnly,
            Cell: ({ row }) => {
                const { id, name, accessibility, guidelineName } = row.original;

                return (
                    <div className="flex items-center gap-1 flex-wrap">
                        <span className={accessibilityDotClasses(accessibility)} />
                        <CellValue
                            value={name}
                            isPending={!!id && edits.getEdit(id, INSTANCE_NAME_FIELD) !== undefined}
                            isReadOnly={accessibility === Accessibility.ReadOnly}
                        />
                        {guidelineName && (
                            <Chip
                                label={guidelineName}
                                size="small"
                                variant="outlined"
                                color="primary"
                                sx={{ fontSize: '0.65rem', height: 18, '& .MuiChip-label': { px: 0.75 } }}
                            />
                        )}
                    </div>
                );
            },
            muiEditTextFieldProps: ({ row }) => ({
                required: true,
                onBlur: bufferEditOnBlur(edits, row.original.id ?? '', INSTANCE_NAME_FIELD, row.original.name),
            }),
        },
        {
            accessorKey: 'classificationName',
            header: 'Instance Class',
            enableEditing: false,
            Cell: ({ row }) => <CellValue value={row.original.classificationName} isReadOnly />,
        },
    ], [edits]);

    return useMaterialReactTable({
        ...tableDefaults,
        columns,
        data: rows,
        layoutMode: 'grid',
        enableColumnResizing: true,
        getRowId: row => row.id ?? '',
        renderTopToolbarCustomActions: ({ table }) => (
            <Box className="flex flex-wrap items-center gap-2">
                {renderToolbarActions?.()}
                <BulkDeleteButton
                    useCaseId={useCaseId}
                    instances={table.getSelectedRowModel().rows.map(row => ({
                        id: row.original.id ?? '',
                        name: row.original.name ?? '',
                    }))}
                    onDeleted={deletedIds => {
                        deletedIds.forEach(id => edits.discardInstance(id));
                        table.resetRowSelection();
                    }}
                />
            </Box>
        ),
        // No row click handler on purpose: a click has to focus the cell, like in a
        // spreadsheet. The action below is the way into the dialog.
        renderRowActions: ({ row }) => (
            <Tooltip title="Open instance">
                <IconButton size="small" onClick={() => row.original.id && onOpenInstance(row.original.id)}>
                    <EditOutlinedIcon fontSize="small" />
                </IconButton>
            </Tooltip>
        ),
    });
};
