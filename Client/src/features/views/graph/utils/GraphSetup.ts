// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/**
 * Options of the instance graph.
 *
 * The canvas is redrawn as a whole on every frame of every pan, zoom and physics step, so
 * whatever is switched on here is paid per node and per edge, per frame. At the node counts a
 * use case reaches this is what decides whether the graph can still be dragged around. Shadows
 * are left off for that reason: they are blurred in a pass of their own and are the single most
 * expensive thing that can be set here.
 */
const graphOptions = {
	physics: {
		// Only there to place nodes that have no remembered position. useNodePositions turns it
		// off as soon as the layout has settled, so a graph that is already laid out runs no
		// solver at all.
		enabled: true,
		barnesHut: {
			theta: 0.5,
			gravitationalConstant: -45100,
			centralGravity: 0.3,
			avoidOverlap: 0.2,
			damping: 0.5,
		},
	},
	layout: {
		// Without the Kamada-Kawai pre-layout, which clusters the whole network before the first
		// draw and costs seconds from a few hundred instances on. The barnesHut run that follows
		// arranges the nodes just as well, and from the second visit the layout comes from
		// storage anyway.
		improvedLayout: false,
	},
	interaction: {
		hover: true,
		// Edges make up the bulk of the draw calls, so they are left out while the view moves.
		// Dragging a node is the exception and keeps them on for the length of the drag, which
		// VisGraphViewer arranges from 'dragStart'.
		hideEdgesOnDrag: true,
		hideEdgesOnZoom: true,
		// Hovering a node would otherwise redraw every edge attached to it in the highlight
		// colour, which the nodes themselves already opt out of below.
		hoverConnectedEdges: true,
	},
	nodes: {
		borderWidth: 2,
		borderWidthSelected: 2,
		widthConstraint: 50,
		labelHighlightBold: false,
		color: {
			hover: {
				background: 'inherit',
				border: 'inherit',
			},
			highlight: {
				background: 'inherit',
				border: 'inherit',
			},
		},
	},
	edges: {
		color: 'black',
	},
};

export default graphOptions;
