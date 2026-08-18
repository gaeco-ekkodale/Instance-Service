// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useEffect, useState } from 'react';
import { Route } from '../../../../routes/instancesRoute';
import { UpdateNode } from '../../../instanceEditor/UpdateNode';
import { CreateNode } from '../../../instanceEditor/CreateNode';
import { GraphModal } from '../../../../components/GraphModal';
import Sidebar from './components/Sidebar';
import { SideBarItem } from './models/SideBarItem';
import { Box } from '@mui/material';
import { useNavigate, useLocation } from 'react-router-dom';

/** Stands in until CreateNode knows whether an instance or a relation is being created. */
const CREATE_MODAL_TITLE = 'Create Instance';

interface NodeConfiguratorProps {
	setCreateNodeMode: (isActive: boolean) => void;
	setTargetNodeId: (nodeId: string | undefined) => void;
	createNodeMode: boolean;
	targetNodeId?: string | null;
}

function NodeConfigurator({ setCreateNodeMode, setTargetNodeId, createNodeMode, targetNodeId }: NodeConfiguratorProps) {
	const [currentlySelectedModal, setCurrentlySelectedModal] = useState<number | boolean>(false);
	const { useCaseId, nodeId } = Route.useSearch();
	const navigate = useNavigate();
	const location = useLocation();

	/*
	 * UseEffect to reset CreateNodeMode states 
	 */
	useEffect(() => {
		if(currentlySelectedModal != CreateNodeItem.id) {
			setCreateNodeMode(false);
			setTargetNodeId(undefined);
		} else if (!nodeId) {
			setTargetNodeId(undefined);
		}
	},[nodeId, currentlySelectedModal]);

	const CreateNodeItem: SideBarItem = {
		iconName: 'Add',
		name: 'Create Instance or Relation',
		description:
			'Click an empty area for a new instance, or click an instance to start a relation and a second one to connect them.',
		id: 0,
		condition: !!useCaseId,
	};

	const UpdateNodeItem: SideBarItem = {
		iconName: 'Edit',
		name: 'Update Instance Information',
		id: 1,
		condition: !!useCaseId && !!nodeId && !createNodeMode,
	};

	const [createModalTitle, setCreateModalTitle] = useState(CREATE_MODAL_TITLE);

	const sidebarItems: SideBarItem[] = [CreateNodeItem, UpdateNodeItem];

	useEffect(() => {
		if(!createNodeMode) {
			if (nodeId) {
				setCurrentlySelectedModal(UpdateNodeItem.id);
			} else {
				setCurrentlySelectedModal(false);
			}
		}	
	}, [nodeId, createNodeMode]);

	const handleChangeItem = (item: number | boolean) => {
		if (item === currentlySelectedModal) {
			setCurrentlySelectedModal(false);
			setCreateNodeMode(false);
		} else {
			setCurrentlySelectedModal(item);
			if(item === CreateNodeItem.id)
				setCreateNodeMode(true);
		}
	};

	const handleCloseModal = () => {
		setCurrentlySelectedModal(CreateNodeItem.id);
		setTargetNodeId(undefined);
        const params = new URLSearchParams(location.search);
        params.delete("nodeId");
        navigate({
            pathname: location.pathname,
            search: `?${params.toString()}`,
        });
    };

	return (
		<Box className={`${useCaseId ? "" : "hidden" }`}>
			<Sidebar sidebarItems={sidebarItems} currentlySelectedModal={currentlySelectedModal} setCurrentlySelectedModal={handleChangeItem} />
			{currentlySelectedModal === CreateNodeItem.id && (targetNodeId || targetNodeId === null) && (
				<GraphModal modalTitle={createModalTitle} className="w-full max-w-3xl" open={currentlySelectedModal === CreateNodeItem.id} onClose={handleCloseModal}>
					<CreateNode setModalTitle={setCreateModalTitle} targetNodeId={targetNodeId} setTargetNodeId={setTargetNodeId} />
				</GraphModal>
			)}
			{currentlySelectedModal === UpdateNodeItem.id && (
				<GraphModal modalTitle="Edit Instance" className="w-full max-w-3xl" open={true} onClose={handleCloseModal}>
					<UpdateNode onClose={handleCloseModal} />
				</GraphModal>
			)}
		</Box>
	);
}

export default NodeConfigurator;
