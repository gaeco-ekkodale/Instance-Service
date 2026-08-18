// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { TourPanel } from './Tour'

export const TOUR_KEY = 'instance'
export const TOUR_MODULE_NAME = 'Instances'

/**
 * Describes this module and its place in gaeco - nothing beyond it. No pointers to other
 * modules or to tools outside the platform.
 *
 * The panel is non-modal, so the steps are written to be carried out while it is open.
 * Kept as data, not JSX, so the wording can be revised without touching a component.
 */
export const TOUR_PANELS: TourPanel[] = [
	{
		title: 'From classifications to objects',
		body: 'The guideline describes classifications in the abstract. Instances are the actual objects behind them — one specific portfolio, its buildings, its rooms — created and connected here as a graph.',
	},
	{
		title: 'Start with a UseCase',
		body: 'Choose one in the toolbar at the top left. The canvas stays empty until you do: the UseCase decides which part of the graph is shown to you, through the access rights configured for it.',
	},
	{
		title: 'Creating an instance',
		body: 'Choose + to enter creator mode, then click an empty area of the canvas. A dialog opens where you pick a classification and fill in its properties.',
	},
	{
		title: 'Creating a relationship',
		body: 'Still in creator mode, click the instance you want to start from — a line then follows your cursor. Click a second instance to connect the two, and a dialog offers the relationships the ontology permits between their classifications.',
	},
	{
		title: 'When a connection is refused',
		body: 'The line only appears if you have full access to the instance you started from. Instances that may not be connected to it show a blocked cursor and ignore the click.',
	},
	{
		title: 'Deleting',
		body: 'Right-click a node or a connecting line. The context menu names what you selected and offers Delete.',
	},
	{
		title: 'The table view',
		body: 'The switch at the top right shows the same instances as a table. Choosing a classification in the table toolbar adds its properties as columns, grouped the same way as in the dialog, and the table can be exported as CSV.',
	},
	{
		title: 'Editing in the table',
		body: 'Double-click a cell to change it, like in a spreadsheet. Edits are collected until you press Save, and Discard drops them. Ticking rows offers to delete them together.',
	},
	{
		title: 'If something is missing',
		body: 'Classifications you cannot select, or properties you cannot edit, are usually not a fault: the access rights of your user group decide what is offered here.',
	},
]
