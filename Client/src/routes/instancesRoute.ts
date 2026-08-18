// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useGraphSearchParams } from '../hooks'
import { getMountPath } from '../utils/routing'

/**
 * Where this microfrontend is mounted and how the views read their filters. Lives apart from
 * the page so that the features reading it do not depend on what renders them.
 *
 * The path is absolute: react-router would resolve a relative one against the current
 * location and append the mount path again on every navigation.
 */
export const Route = {
	path: getMountPath() || '/',
	useSearch: useGraphSearchParams,
}
