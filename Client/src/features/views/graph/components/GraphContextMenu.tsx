// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box, Chip, Divider, ListItemIcon, ListItemText, MenuItem, Paper, Typography } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import BlockIcon from '@mui/icons-material/Block';
import { ContextMenuState } from '../hooks/useGraphContextMenu';

interface GraphContextMenuProps {
	menuState: ContextMenuState;
	canDelete: boolean;
	onDeleteNode: () => void;
	onDeleteRelation: () => void;
}

const truncateUrl = (url?: string | null) =>
	url?.split(/[/#]/).filter(Boolean).pop() ?? '';

const displayName = (instance?: { name?: string | null; classificationName?: string | null }) =>
	instance?.name || instance?.classificationName || '–';

export function GraphContextMenu({ menuState, canDelete, onDeleteNode, onDeleteRelation }: GraphContextMenuProps) {
	const { x, y, type, targetInstance, objectInstance, relation } = menuState;
	const handleDelete = type === 'node' ? onDeleteNode : onDeleteRelation;

	return (
		<Paper
			elevation={8}
			onMouseDown={e => e.stopPropagation()}
			sx={{ position: 'absolute', left: x, top: y, zIndex: 1300, minWidth: 220, maxWidth: 320, py: 0.5, overflow: 'hidden' }}
		>
			{/* Header */}
			<Typography
				variant="overline"
				sx={{ px: 1.5, lineHeight: 2, display: 'block', fontSize: '0.6rem', color: 'text.disabled' }}
			>
				{type === 'node' ? 'Node' : 'Relation'}
			</Typography>

			{type === 'node' ? (
				/* ── Node info ── */
				<Box sx={{ px: 1.5, pb: 1 }}>
					{targetInstance?.guidelineName && (
						<Chip
							label={targetInstance.guidelineName}
							size="small"
							color="primary"
							variant="outlined"
							sx={{ fontSize: '0.65rem', height: 18, mb: 0.5 }}
						/>
					)}
					<Typography variant="body2" fontWeight={600} noWrap title={targetInstance?.classificationName ?? undefined}>
						{targetInstance?.classificationName ?? '–'}
					</Typography>
					{targetInstance?.name && targetInstance.name !== targetInstance.classificationName && (
						<Typography variant="caption" color="text.secondary" noWrap title={targetInstance.name}>
							{targetInstance.name}
						</Typography>
					)}
				</Box>
			) : (
				/* ── Relation info ── */
				<Box sx={{ px: 1.5, pb: 1 }}>
					<Box sx={{ display: 'flex', alignItems: 'baseline', gap: 0.5, flexWrap: 'wrap' }}>
						<Typography variant="body2" fontWeight={600} noWrap title={displayName(targetInstance)}>
							{displayName(targetInstance)}
						</Typography>
						<Typography variant="caption" color="text.disabled" sx={{ mx: 0.25 }}>→</Typography>
						<Typography
							variant="caption"
							color="primary.main"
							noWrap
							title={relation?.label ?? undefined}
							sx={{ fontStyle: 'italic' }}
						>
							{truncateUrl(relation?.label)}
						</Typography>
						<Typography variant="caption" color="text.disabled" sx={{ mx: 0.25 }}>→</Typography>
						<Typography variant="body2" fontWeight={600} noWrap title={displayName(objectInstance)}>
							{displayName(objectInstance)}
						</Typography>
					</Box>
				</Box>
			)}

			<Divider />

			<MenuItem
				dense
				disabled={!canDelete}
				onClick={canDelete ? handleDelete : undefined}
				sx={{ mt: 0.25, color: canDelete ? 'error.main' : 'text.disabled' }}
			>
				<ListItemIcon>
					{canDelete
						? <DeleteIcon fontSize="small" color="error" />
						: <BlockIcon fontSize="small" />
					}
				</ListItemIcon>
				<ListItemText
					primary={canDelete ? 'Delete' : 'Access restricted'}
					primaryTypographyProps={{ fontSize: 14 }}
				/>
			</MenuItem>
		</Paper>
	);
}
