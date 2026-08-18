// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useNavigate } from 'react-router-dom';
import { createSearchParams } from 'react-router-dom';
import { Route } from '../../../routes/instancesRoute';
import {
	type InstanceService_Api_Dto_Ontology_RelationDTO as RelationDTO,
	InstanceService_Api_Dto_Request_CreateInstance as CreateNodeDto,
	InstanceService_Models_Enum_Direction as Direction,
	InstanceService_Api_Dto_InstanceRelation as InstanceRelation,
	InstanceService_Api_Dto_Request_CreateRelation as CreateRelation,
} from '../../../services/instance';
import { Classification } from '../../../services/access';
import { useCheckRelationExists, useGraphSearchParams } from '../../../hooks';

interface UseHandleCreateParams {
	relations?: InstanceRelation[] | null;
	createNodeHandler: (node: CreateNodeDto) => Promise<any>;
	createRelationHandler: (relation: CreateRelation) => Promise<any>;
	// Form state
	relation: RelationDTO | null;
	selectedDirection?: Direction | null;
	selectedClassification: Classification | null;
	newName: string;
	newProperties: Record<string, any>;
	hasNoTargetNode: boolean;
	targetNodeId?: string | null;
	targetNodeMetadata?: any;
	setTargetNodeId: (nodeId: string | undefined) => void;
}

export const useHandleCreate = ({
	relations,
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
}: UseHandleCreateParams) => {
	const navigate = useNavigate();
	const { useCaseId, nodeId, textQuery } = useGraphSearchParams();
	const { checkRelationExists } = useCheckRelationExists({ relations });

	const resetSearch = (): void => {
		const searchParams = {
			useCaseId: `${useCaseId}`,
			...(textQuery && { textQuery: `${textQuery}` }),
		};
		const paramsObj = createSearchParams(searchParams);

		navigate({
			pathname: Route.path,
			search: `?${paramsObj.toString()}`,
		});
	};

	// Case 1: Create a new node without any relation
	const handleCreateNode = async (): Promise<void> => {
		const newNode: CreateNodeDto = {
			classificationId: selectedClassification?.id,
			name: newName,
			properties: newProperties,
		};
		await createNodeHandler(newNode);
		setTargetNodeId(undefined);
	};

	// Case 2: Create relation between two existing nodes
	const handleCreateRelationBetweenExistingNodes = async (): Promise<void> => {
		// The relation is identified by the ontology property URI, so all three parts are required.
		if (!nodeId || !targetNodeId || !relation?.predicateId) return;

		const direction = selectedDirection ?? Direction.From;

		const relationRequest: CreateRelation =
			direction === Direction.From
				? {
					subjectId: nodeId,
					objectId: targetNodeId,
					predicateUri: relation.predicateId,
				}
				: {
					subjectId: targetNodeId,
					objectId: nodeId,
					predicateUri: relation.predicateId,
				};

		await createRelationHandler(relationRequest);
		resetSearch();
	};

	// Case 3: Create a new node with a relation to the source node
	const handleCreateNodeWithRelation = async (): Promise<void> => {
		const newNode: CreateNodeDto = {
			classificationId: selectedClassification?.id ?? targetNodeMetadata?.classificationId,
			name: newName,
			properties: newProperties,
		};

		const direction = selectedDirection ?? Direction.From;

		newNode.relation = {
			instanceId: nodeId!,
			predicateUri: relation?.predicateId!,
			direction: direction,
		};

		await createNodeHandler(newNode);
		resetSearch();
	};

	const handleCreate = async (): Promise<void> => {
		if (relation && nodeId) {
			if (checkRelationExists(nodeId, targetNodeId, relation.predicateId, 'warning') ||
				checkRelationExists(targetNodeId, nodeId, relation.predicateId, 'warning')) {
				return;
			}

			if (!hasNoTargetNode) {
				await handleCreateRelationBetweenExistingNodes();
			} else {
				await handleCreateNodeWithRelation();
			}
		} else if (targetNodeId === null && selectedClassification) {
			await handleCreateNode();
		}
	};

	return { handleCreate };
};
