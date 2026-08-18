// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { RefObject, useCallback, useEffect, useRef, useState } from 'react';
import {
	InstanceService_Models_Enum_Accessibility as Accessibility,
	InstanceService_Api_Dto_Instance as Instance,
} from '../../../../services/instance';

interface UseCursorLineProps {
	visNetwork: any;
	containerRef: RefObject<HTMLDivElement>;
	canvasRef: RefObject<HTMLCanvasElement>;
	nodeId: string | null;
	createNodeMode: boolean;
	allInstances: Instance[];
}

interface Position {
	x: number;
	y: number;
}

/**
 * Sets up the canvas dimensions to match the container and clears it.
 * @returns The 2D rendering context if available, null otherwise.
 */
const setupCanvas = (
	canvas: HTMLCanvasElement,
	container: HTMLDivElement
): CanvasRenderingContext2D | null => {
	const ctx = canvas.getContext('2d');
	if (!ctx) return null;

	canvas.width = container.clientWidth;
	canvas.height = container.clientHeight;
	ctx.clearRect(0, 0, canvas.width, canvas.height);

	return ctx;
};

/**
 * Draws a line from the node position to the mouse cursor position.
 */
const drawCursorLine = (
	ctx: CanvasRenderingContext2D,
	fromPos: Position,
	toPos: Position
): void => {
	ctx.beginPath();
	ctx.moveTo(fromPos.x, fromPos.y);
	ctx.lineTo(toPos.x, toPos.y);
	ctx.strokeStyle = '#3e3e3e';
	ctx.lineWidth = 4;
	ctx.stroke();
};

/**
 * Hook that manages the cursor line drawing from a selected node to the mouse cursor.
 * Handles mouse tracking, canvas drawing, and updates during zoom/pan/physics simulation.
 */
export const useCursorLine = ({
	visNetwork,
	containerRef,
	canvasRef,
	nodeId,
	createNodeMode,
	allInstances,
}: UseCursorLineProps): void => {
	const [mousePos, setMousePos] = useState<{ x: number; y: number } | null>(null);
	const [positionTrigger, setPositionTrigger] = useState<number>(0);

	const allInstancesRef = useRef(allInstances);
	allInstancesRef.current = allInstances;

	/**
	 * Check if the current source node has FullControl accessibility.
	 * The cursor line should only be drawn if the source node has FullControl.
	 */
	const sourceNodeHasFullControl = useCallback(() => {
		if (!nodeId || !createNodeMode) return false;
		const sourceNode = allInstancesRef.current.find((node) => node.id === nodeId);
		return sourceNode?.accessibility === Accessibility.FullControl;
	}, [nodeId, createNodeMode]);

	/**
	 * Get the position of the selected node in DOM coordinates.
	 * Converts from vis.js canvas coordinates to DOM coordinates.
	 */
	const getSelectedNodePosition = useCallback(() => {
		if (!visNetwork || !nodeId) return null;

		try {
			const nodePositions = visNetwork.getPositions([nodeId]);
			if (!nodePositions || !nodePositions[nodeId]) return null;

			const nodePos = nodePositions[nodeId];
			const domPos = visNetwork.canvasToDOM({ x: nodePos.x, y: nodePos.y });
			return domPos;
		} catch {
			return null;
		}
	}, [visNetwork, nodeId]);

	/**
	 * Handle mouse movement for tracking cursor position relative to the container.
	 */
	useEffect(() => {
		if (!nodeId || !containerRef.current || !createNodeMode) return;

		if (!sourceNodeHasFullControl()) return;

		const handleMouseMove = (e: MouseEvent) => {
			const container = containerRef.current;
			if (!container) return;

			const rect = container.getBoundingClientRect();
			setMousePos({
				x: e.clientX - rect.left,
				y: e.clientY - rect.top,
			});
		};

		const container = containerRef.current;
		container.addEventListener('mousemove', handleMouseMove);

		return () => {
			container.removeEventListener('mousemove', handleMouseMove);
			setMousePos(null);
		};
	}, [nodeId, createNodeMode, sourceNodeHasFullControl, containerRef]);

	/**
	 * Handle zoom and drag events.
	 * Updates the position trigger to re-render the cursor line.
	 */
	useEffect(() => {
		if (!visNetwork || !nodeId || !createNodeMode) return;

		const updatePosition = () => {
			setPositionTrigger((prev) => prev + 1);
		};

		visNetwork.on('zoom', updatePosition);
		visNetwork.on('dragEnd', updatePosition);
		visNetwork.on('dragging', updatePosition);

		return () => {
			visNetwork.off('zoom', updatePosition);
			visNetwork.off('dragEnd', updatePosition);
			visNetwork.off('dragging', updatePosition);
		};
	}, [visNetwork, nodeId, createNodeMode]);

	/**
	 * Draw the line from the selected node to the cursor position.
	 * Re-renders whenever mouse position or positionTrigger changes.
	 */
	useEffect(() => {
		const canvas = canvasRef.current;
		const container = containerRef.current;
		if (!canvas || !container) return;

		const ctx = setupCanvas(canvas, container);
		if (!ctx) return;

		if (!nodeId || !mousePos || !visNetwork || !createNodeMode || !sourceNodeHasFullControl()) return;

		const nodePos = getSelectedNodePosition();
		if (!nodePos) return;

		drawCursorLine(ctx, nodePos, mousePos);
	}, [
		mousePos,
		nodeId,
		visNetwork,
		getSelectedNodePosition,
		positionTrigger,
		createNodeMode,
		sourceNodeHasFullControl,
		canvasRef,
		containerRef,
	]);
};
