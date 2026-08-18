// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { ReactNode, useMemo } from 'react'
import { useGraphChangeNotifications } from '../../services/signalR/useGraphChangeNotifications'
import { InstancesContext } from './instancesContext'
import { useInstanceData } from './useInstanceData'
import { useInstanceEdits } from './useInstanceEdits'

/**
 * Owns the state the graph and the table both render, so that the two are views of one thing
 * instead of two features that happen to fetch the same data. Everything below reads it from
 * the context, which is also how the edit dialog reaches the buffer from either view.
 */
export const InstancesProvider = ({ children }: Readonly<{ children: ReactNode }>) => {
	const data = useInstanceData()
	const edits = useInstanceEdits(data.useCaseId)
	const instances = useMemo(() => ({ ...data, edits }), [data, edits])

	// Here rather than in a view: both of them show the same instances, and neither is
	// guaranteed to be mounted while the other is looked at.
	useGraphChangeNotifications(data.useCaseId)

	return <InstancesContext.Provider value={instances}>{children}</InstancesContext.Provider>
}
