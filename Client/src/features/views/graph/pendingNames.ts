// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { InstanceService_Api_Dto_Graph as Graph } from '../../../services/instance'
import { INSTANCE_NAME_FIELD, type InstanceEdits } from '../../instances/useInstanceEdits'

/**
 * Puts the buffered names into a copy of the graph, so the canvas shows the same values as
 * the table. As long as no name is buffered - the case while any other field is edited -
 * the graph keeps its identity and with it the vis-network data effect at rest.
 */
export const applyPendingNames = (graph: Graph | undefined, edits: InstanceEdits): Graph | undefined => {
	const pendingNames = new Map(
		[...edits.editedInstanceIds]
			.map(id => [id, edits.getEdit(id, INSTANCE_NAME_FIELD)] as [string, string | undefined])
			.filter(([, name]) => name !== undefined),
	)

	if (!graph?.instances || pendingNames.size === 0) return graph

	return {
		...graph,
		instances: graph.instances.map(instance =>
			instance.id && pendingNames.has(instance.id)
				? { ...instance, name: pendingNames.get(instance.id) }
				: instance,
		),
	}
}
