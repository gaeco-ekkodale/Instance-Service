// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {
	Box,
	Button,
	Chip,
	Dialog,
	DialogActions,
	DialogContent,
	DialogContentText,
	DialogTitle,
	Divider,
	LinearProgress,
	Skeleton,
} from '@mui/material';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import UndoOutlinedIcon from '@mui/icons-material/UndoOutlined';
import { useMemo, useState } from 'react';
import { Route } from '../../routes/instancesRoute';
import { InstanceService_Models_Enum_Accessibility as Accessibility } from '../../services/instance';
import GuidelinePropertyEditor from './GuidelinePropertyEditor';
import { createSearchParams, useNavigate } from 'react-router-dom';
import { useGetInstance, useGetGraph } from '../../hooks';
import { useDeleteInstance } from './hooks';
import { InstanceNameInputField } from './components/InstanceNameInputField';
import { useInstances } from '../instances/instancesContext';
import { INSTANCE_NAME_FIELD, normalizeEditValue } from '../instances/useInstanceEdits';
import { pendingSx } from '../instances/pendingSx';

interface UpdateNodeProps {
	/** Closes the dialog, both on Close and after deleting. */
	onClose: () => void;
}

function PropertiesSkeleton() {
	return (
		<Box sx={{ px: 1 }}>
			<LinearProgress sx={{ mb: 1.5, borderRadius: 1 }} />
			{[...Array(6)].map((_, i) => (
				<Skeleton key={i} variant="rounded" height={36} sx={{ mb: 0.5 }} />
			))}
		</Box>
	);
}

/**
 * Edits one instance. The dialog holds no values of its own: it is a second editor of the
 * page's edit buffer, just like a table cell, so every change is kept when it closes and
 * is written to the server by Save in the header. Deleting is immediate - it cannot be
 * expressed as a buffered value.
 */
export const UpdateNode = ({ onClose }: UpdateNodeProps) => {
	const navigate = useNavigate();
	const [deleteConfirmOpen, setDeleteConfirmOpen] = useState<boolean>(false);
	const { useCaseId, nodeId, classificationId, textQuery } = Route.useSearch();

	const { edits } = useInstances();
	const instanceId = nodeId ?? '';

	const { data: node, isLoading } = useGetInstance(nodeId, useCaseId);

	const { data: graph } = useGetGraph(useCaseId);

	/** What is buffered for this instance, no matter which view produced it. */
	const bufferedFields = edits.getInstanceEdits(instanceId);
	const hasBufferedFields = Object.keys(bufferedFields).length > 0;

	const shownName = bufferedFields[INSTANCE_NAME_FIELD] ?? node?.name ?? '';

	/** Stored values with the buffered ones on top, as the property editor expects them. */
	const shownProperties = useMemo(() => {
		const values: Record<string, unknown> = {};

		node?.properties?.forEach(property => {
			if (property.name) values[property.name] = property.value;
		});
		Object.entries(bufferedFields).forEach(([field, value]) => {
			if (field !== INSTANCE_NAME_FIELD) values[field] = value;
		});

		return values;
	}, [node, bufferedFields]);

	/** Drops the opened instance from the URL, keeping the filters of the current view. */
	const resetSearch = () => {
		if (!useCaseId) return;

		const params = createSearchParams({
			useCaseId: `${useCaseId}`,
			...(classificationId && { classificationId }),
			...(textQuery && { textQuery }),
		});

		navigate({
			pathname: Route.path,
			search: `?${params.toString()}`,
		});
	};

	const storedValue = (field: string) =>
		field === INSTANCE_NAME_FIELD
			? node?.name
			: node?.properties?.find(property => property.name === field)?.value;

	/**
	 * Buffers one field. A value edited back to the stored one is dropped from the buffer
	 * instead of being counted as an unsaved change.
	 */
	const bufferField = (field: string, value: unknown) => {
		if (!instanceId) return;

		if (normalizeEditValue(value) === normalizeEditValue(storedValue(field))) {
			edits.clearEdit(instanceId, field);
		} else {
			edits.setEdit(instanceId, field, value);
		}
	};

	/** The property editor reports its whole map, so the changed fields are picked out. */
	const bufferProperties = (next: Record<string, unknown>) =>
		Object.entries(next)
			.filter(([field, value]) => normalizeEditValue(value) !== normalizeEditValue(shownProperties[field]))
			.forEach(([field, value]) => bufferField(field, value));

	const { mutateAsync: deleteNodeHandler } = useDeleteInstance(nodeId, useCaseId);

	const handleDeleteNode = async () => {
		setDeleteConfirmOpen(false);
		await deleteNodeHandler();
		// The instance is gone, so a later save must not try to write its buffered values.
		edits.discardInstance(instanceId);
		onClose();
		resetSearch();
	};

	const encodedClassificationURI = node?.classificationId
		? encodeURIComponent(node.classificationId)
		: '';

	// The graph node carries the accessibility and the guideline of the classification,
	// neither of which is part of the instance metadata.
	const graphInstance = graph?.instances?.find(instance => instance.id === nodeId);
	const nodeAccessibility = graphInstance?.accessibility ?? Accessibility.None;

	const writeRight = nodeAccessibility === Accessibility.FullControl || nodeAccessibility === Accessibility.ReadWrite;
	const hasFilledReadOnlyProperty = node?.properties?.some(p => p.isReadOnly === true && !!p.value) ?? false;
	const deleteRight = !isLoading && node != null && writeRight && !hasFilledReadOnlyProperty;

	/** Without write rights every field is shown locked, so nothing can be buffered. */
	const editorProperties = useMemo(
		() =>
			writeRight
				? node?.properties ?? undefined
				: node?.properties?.map(property => ({ ...property, isReadOnly: true })),
		[node, writeRight],
	);

	return (
		<Box className="flex flex-col min-h-0 flex-1">
			<Box className="sticky top-0 bg-white z-10 shrink-0">
				<Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap', mb: 1 }}>
					{node?.classificationName && (
						<Chip label={node.classificationName} size="small" color="secondary" />
					)}
					{graphInstance?.guidelineName && (
						<Chip label={graphInstance.guidelineName} size="small" variant="outlined" />
					)}
				</Box>
				{/* Locked while loading: without the stored name, typing here could not be
				    told apart from an edit and would be buffered as one. */}
				<Box sx={{ p: 0.5, ...(INSTANCE_NAME_FIELD in bufferedFields && pendingSx) }}>
					<InstanceNameInputField
						label="InstanceName"
						value={shownName}
						isReadonly={!writeRight || isLoading}
						onChange={name => bufferField(INSTANCE_NAME_FIELD, name)}
					/>
				</Box>
				<Divider sx={{ mt: 1.5 }} />
			</Box>

			<Box className="flex-1 min-h-0 overflow-auto" sx={{ pt: 1 }}>
				{isLoading && <PropertiesSkeleton />}

				{!isLoading && node?.properties && (
					<GuidelinePropertyEditor
						key={nodeId}
						classificationId={encodedClassificationURI}
						useCaseId={useCaseId!}
						currentValues={shownProperties}
						onPropertiesChange={bufferProperties}
						metadataProperties={editorProperties}
						pendingProperties={new Set(Object.keys(bufferedFields))}
					/>
				)}
			</Box>

			<Box className="shrink-0 flex items-center gap-2" mt={2}>
				{/* Relations are deleted individually by right-clicking the connection in the graph. */}
				{deleteRight && (
					<Button
						variant="contained"
						color="error"
						startIcon={<DeleteOutlineIcon />}
						onClick={() => setDeleteConfirmOpen(true)}
					>
						Delete
					</Button>
				)}
				<Box className="ml-auto flex items-center gap-2">
					{hasBufferedFields && (
						<Button
							variant="outlined"
							startIcon={<UndoOutlinedIcon />}
							onClick={() => edits.discardInstance(instanceId)}
						>
							Reset
						</Button>
					)}
					<Button variant="contained" color="secondary" onClick={onClose}>
						Close
					</Button>
				</Box>
			</Box>

			<Dialog open={deleteConfirmOpen} onClose={() => setDeleteConfirmOpen(false)}>
				<DialogTitle>Delete instance?</DialogTitle>
				<DialogContent>
					<DialogContentText>
						{node?.name ? `"${node.name}"` : 'This instance'} and all its connections are
						removed permanently.
					</DialogContentText>
				</DialogContent>
				<DialogActions>
					<Button onClick={() => setDeleteConfirmOpen(false)}>Cancel</Button>
					<Button variant="contained" color="error" onClick={handleDeleteNode}>
						Delete
					</Button>
				</DialogActions>
			</Dialog>
		</Box>
	);
};
