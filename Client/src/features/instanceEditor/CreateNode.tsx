// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box, Button, Chip, Typography, Collapse } from '@mui/material';
import { useCallback, useEffect, useMemo, useState } from 'react';
import ClassificationSearch from './components/ClassificationSearch';
import { Route } from '../../routes/instancesRoute';
import { Classification } from '../../services/access';
import { CreateWithRelationship } from './components/CreateWithRelationship';
import {
	InstanceService_Models_Enum_Direction as Direction,
	type InstanceService_Api_Dto_Ontology_RelationDTO as RelationDTO,
} from '../../services/instance';
import GuidelinePropertyEditor from './GuidelinePropertyEditor';
import { ExpandMore, ExpandLess } from '@mui/icons-material';
import { useGetClassifications, useGetGraph, useGetInstances, useConnectableClassIds } from '../../hooks';
import { useCreateInstance, useCreateRelation, useHandleCreate } from './hooks';
import { InstanceNameInputField } from './components/InstanceNameInputField';

interface CreateNodeProps {
	setModalTitle: (name: string) => void;
	targetNodeId?: string | null;
	setTargetNodeId: (nodeId: string | undefined) => void;
}

export const CreateNode = ({ setModalTitle, targetNodeId, setTargetNodeId }: CreateNodeProps) => {
	const { useCaseId, nodeId } = Route.useSearch();
	const [relation, setRelationState] = useState<RelationDTO | null>(null);
	const [selectedDirection, setSelectedDirection] = useState<Direction | null>(null);
	const [hasNoTargetNode, setHasNoTargetNode] = useState<boolean>(true);

	const { data: classifications } = useGetClassifications(useCaseId);

	const setRelation = useCallback((relationDto: RelationDTO | null, direction?: Direction) => {
		setRelationState(relationDto);
		if (direction) setSelectedDirection(direction);
	}, []);

	/**
	 * Query to fetch all metadata of origin node and target node.
	 * Filters out falsy IDs to avoid sending null/undefined to the API.
	 */
	const instanceIds = useMemo(() => {
		if (!nodeId) return undefined;
		return [targetNodeId, nodeId].filter(Boolean) as string[];
	}, [nodeId, targetNodeId]);

	const { data: nodesMetaData } = useGetInstances(useCaseId, instanceIds);

	const targetNodeMetadata = nodesMetaData?.find(n => n.id === targetNodeId);
	const sourceNodeMetadata = nodesMetaData?.find(n => n.id === nodeId);

	const { connectableClassIds } = useConnectableClassIds(
		useCaseId,
		nodeId,
		!!nodeId && !targetNodeId,
	);

	const availableClassifications = useMemo(() => {
		const all = classifications?.classifications ?? [];
		if (!nodeId || !connectableClassIds) return all;
		return all.filter(c => c.id && connectableClassIds.has(c.id));
	}, [classifications, connectableClassIds, nodeId]);

	const [selectedClassification, setSelectedClassification] = useState<Classification | null>(null);
	const [newProperties, setNewProperties] = useState<Record<string, any>>({});
	const [newName, setNewName] = useState<string>('');

	useEffect(() => {
		if (targetNodeMetadata && sourceNodeMetadata) {
			setHasNoTargetNode(false);
			setModalTitle(`Create Relation from ${sourceNodeMetadata.classificationName} to ${targetNodeMetadata.classificationName}`);
			setNewName(targetNodeMetadata.classificationName ?? '');
		} else if (selectedClassification && selectedClassification.name) {
			setHasNoTargetNode(true);
			setModalTitle(`Create ${selectedClassification.name}`);
			setNewName(selectedClassification.name);
		} else {
			setModalTitle('Create Instance');
		}
	}, [selectedClassification, setModalTitle, targetNodeMetadata, sourceNodeMetadata]);

	const [open, setOpen] = useState(true);
	const [topCollapsed, setTopCollapsed] = useState(false);

	useEffect(() => {
		const classOk = !hasNoTargetNode || !!selectedClassification;
		const relationOk = !nodeId || !sourceNodeMetadata?.classificationId || !!relation;
		setTopCollapsed(classOk && relationOk && (!!selectedClassification || !hasNoTargetNode));
	}, [selectedClassification, relation, hasNoTargetNode, nodeId, sourceNodeMetadata?.classificationId]);

	const { mutateAsync: createNodeHandler } = useCreateInstance(useCaseId);
	const { mutateAsync: createRelationHandler } = useCreateRelation(useCaseId);

	const { data: graphNodesData } = useGetGraph(useCaseId);

	const { handleCreate } = useHandleCreate({
		relations: graphNodesData?.relations,
		createNodeHandler,
		createRelationHandler,
		relation,
		selectedDirection,
		selectedClassification,
		newName,
		newProperties,
		hasNoTargetNode,
		targetNodeId,
		targetNodeMetadata,
		setTargetNodeId,
	});

	const encodedClassificationId = encodeURIComponent(selectedClassification?.id!);
	const encodedTargetNodeClassificationId = encodeURIComponent(targetNodeMetadata?.classificationId!);

	return (
		<Box className="flex flex-col min-h-0 flex-1 p-2 w-full max-w-7xl mx-auto">
			{/* Collapsible type & connection section */}
			<Box className="shrink-0">
				<Box
					onClick={() => setTopCollapsed(p => !p)}
					sx={{
						display: 'flex', alignItems: 'center', gap: 0.75,
						cursor: 'pointer', px: 0.5, py: 0.5, mb: 0.5, borderRadius: 1,
						'&:hover': { bgcolor: 'action.hover' },
					}}
				>
					<Box sx={{ flex: 1, display: 'flex', gap: 0.5, flexWrap: 'wrap', alignItems: 'center' }}>
						{topCollapsed ? (
							<>
								{selectedClassification && (
									<Chip label={selectedClassification.name} size="small" color="secondary" />
								)}
								{!hasNoTargetNode && targetNodeMetadata && (
									<Chip label={targetNodeMetadata.classificationName ?? ''} size="small" color="secondary" />
								)}
								{relation && (
									<Chip
										label={relation.label || (relation.predicateId?.split(/[/#]/).filter(Boolean).pop() ?? '')}
										size="small"
										variant="outlined"
									/>
								)}
							</>
						) : (
							<Typography variant="caption" color="text.secondary">
								Type & Connection
							</Typography>
						)}
					</Box>
					{topCollapsed ? <ExpandMore sx={{ fontSize: 18, color: 'text.secondary' }} /> : <ExpandLess sx={{ fontSize: 18, color: 'text.secondary' }} />}
				</Box>
				<Collapse in={!topCollapsed}>
					<Box sx={{ height: hasNoTargetNode ? 240 : 'auto' }} className="flex flex-row gap-2">
						{hasNoTargetNode && (
							<Box sx={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column' }}>
								<ClassificationSearch
									classList={availableClassifications}
									setSelectedClassification={setSelectedClassification}
								/>
							</Box>
						)}
						{(nodeId && sourceNodeMetadata?.classificationId) && (
							<Box sx={{ flex: 1, minWidth: 0, display: 'flex', alignItems: hasNoTargetNode ? 'flex-start' : 'center' }}>
								<CreateWithRelationship
									selectedClassificationId={hasNoTargetNode ? encodedClassificationId : encodedTargetNodeClassificationId}
									setRelation={setRelation}
									sourceNodeClassificationId={sourceNodeMetadata.classificationId}
								/>
							</Box>
						)}
					</Box>
				</Collapse>
			</Box>
			{selectedClassification && (
				<>
					<Box className="shrink-0">
						<InstanceNameInputField 
							label="InstanceName" 
							value={newName} 
							isReadonly={false} 
							onChange={setNewName}
						/>
					</Box>
					<Box className="flex-1 min-h-0 overflow-auto">
						<Button
							variant="text"
							fullWidth
							onClick={() => setOpen(!open)}
							endIcon={open ? <ExpandLess /> : <ExpandMore />}
							className="text-left font-normal border-b border-gray-300"
						>
							<Typography variant="body1">Attributes</Typography>
						</Button>
						<Collapse in={open}>
							<Box className="pt-1">
								{selectedClassification && encodedClassificationId && selectedClassification.name && (
									<GuidelinePropertyEditor
										key={encodedClassificationId}
										classificationId={encodedClassificationId}
										useCaseId={useCaseId!}
										onPropertiesChange={setNewProperties}
									/>
								)}
							</Box>
						</Collapse>
					</Box>
				</>
			)}
			<Box className="shrink-0 flex flex-row justify-end mt-2">
                <Button variant="contained" color="secondary" onClick={handleCreate} disabled={!hasNoTargetNode && !relation}>
                    {`Create ${hasNoTargetNode ? 'Node' : 'Relation'}`}
                </Button>
            </Box>
		</Box>
	);
};