// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useState } from 'react';
import { createSearchParams, useNavigate } from 'react-router-dom';
import { GraphModal } from '../../../components/GraphModal';
import { CreateNode } from '../../instanceEditor/CreateNode';
import { UpdateNode } from '../../instanceEditor/UpdateNode';
import { Route } from '../../../routes/instancesRoute';

interface TableInstanceDialogsProps {
	createOpen: boolean;
	onCloseCreate: () => void;
}

/**
 * Create and edit dialogs of the table view. They are the same dialogs the graph view
 * uses, so property sets and access rights behave identically in both views.
 */
export const TableInstanceDialogs = ({ createOpen, onCloseCreate }: Readonly<TableInstanceDialogsProps>) => {
	const navigate = useNavigate();
	const { useCaseId, nodeId, classificationId, textQuery } = Route.useSearch();
	const [createTitle, setCreateTitle] = useState<string>('Create Instance');

	/** Drops the opened instance from the URL, keeping the table filters. */
	const closeInstance = () => {
		const searchParams = {
			useCaseId: `${useCaseId}`,
			...(classificationId && { classificationId }),
			...(textQuery && { textQuery }),
		};

		navigate({
			pathname: Route.path,
			search: `?${createSearchParams(searchParams).toString()}`,
		});
	};

	if (createOpen) {
		return (
			<GraphModal modalTitle={createTitle} className="w-full max-w-3xl" open onClose={onCloseCreate}>
				{/* targetNodeId null means "a standalone instance" - relations are drawn in the graph. */}
				<CreateNode
					setModalTitle={setCreateTitle}
					targetNodeId={null}
					setTargetNodeId={onCloseCreate}
				/>
			</GraphModal>
		);
	}

	if (!nodeId) return null;

	return (
		<GraphModal modalTitle="Edit Instance" className="w-full max-w-3xl" open onClose={closeInstance}>
			<UpdateNode onClose={closeInstance} />
		</GraphModal>
	);
};
