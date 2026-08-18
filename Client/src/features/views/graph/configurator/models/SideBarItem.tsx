// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

export interface SideBarItem {
	// The icon name for the item
	iconName: string;
	// name of the item
	name: string;
	// what the item is for, shown under the name in its tooltip
	description?: string;
	// id of the item
	id: number;
	// condition to show the item
	condition: boolean;
}
