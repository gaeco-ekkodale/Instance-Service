// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { createContext, useContext } from 'react'
import type { InstanceEdits } from './useInstanceEdits'
import type { useInstanceData } from './useInstanceData'

/** Everything the views render: the instances under their filters, plus the unsaved edits. */
export type Instances = ReturnType<typeof useInstanceData> & { edits: InstanceEdits }

export const InstancesContext = createContext<Instances | null>(null)

/**
 * Reads the shared instance state. Both views and the edit dialog take it from here rather
 * than from props, so none of them can end up showing a different set of instances.
 */
export const useInstances = (): Instances => {
	const instances = useContext(InstancesContext)
	if (!instances) throw new Error('useInstances needs an InstancesProvider above it')
	return instances
}
