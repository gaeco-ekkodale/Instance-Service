// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/** Marks a value that is not saved yet. */
export const pendingSx = { backgroundColor: 'warning.light', borderRadius: 0.5 }

/** The same mark inside a table cell, where the text needs room around it. */
export const pendingCellSx = { ...pendingSx, px: 0.5 }
