// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import VisGraph from 'react-vis-graph-wrapper';
import { useCallback, useEffect, useMemo, useState, useRef } from 'react';
import { useGraphData } from '../hooks/useGraphData';
import { useCursorLine } from '../hooks/useCursorLine';
import { useGraphSearch } from '../hooks/useGraphSearch';
import { useCreateClickHandler } from '../hooks/useCreateClickHandler';
import { useGraphContextMenu } from '../hooks/useGraphContextMenu';
import { useNodePositions } from '../hooks/useNodePositions';
import { GraphContextMenu } from './GraphContextMenu';
import { GraphLayoutButton } from './GraphLayoutButton';
import graphOptions from '../utils/GraphSetup';
import {
	InstanceService_Api_Dto_Graph as Graph,
	InstanceService_Api_Dto_Instance as Instance,
} from '../../../../services/instance';
import { Route } from '../../../../routes/instancesRoute';

/**
 * Zoom below which the guideline chips are left out. Their text is 9px in graph coordinates,
 * so from here on it is unreadable and only costs frames.
 */
const MIN_CHIP_SCALE = 0.35;

interface VisGraphViewerProps {
	onClickGraph: (simpleInstance: Instance | undefined) => void;
	/** Undefined while a graph is loading; the network then keeps what it shows. */
	graphData?: Graph;
	useCaseId: string;
	createNodeMode: boolean;
	connectableClassIds?: Set<string> | null;
	/** Instances holding unsaved values, marked on the canvas like a pending table cell. */
	pendingInstanceIds?: Set<string> | null;
}

/**
 * Renders a graph view component that displays a network graph with interactive features.
 * The graph is populated with nodes and edges based on provided instances and can be searched
 * and filtered by instance names. Nodes matching the search term are highlighted.
 * @returns {JSX.Element} The GraphView component.
 */
export default function VisGraphViewer({
	onClickGraph,
	graphData,
	useCaseId,
	createNodeMode,
	connectableClassIds,
	pendingInstanceIds,
}: Readonly<VisGraphViewerProps>) {
	const [allInstances, setAllInstances] = useState<Instance[]>([]);
	const [isHoveringNode, setIsHoveringNode] = useState(false);
	const [hoveredNodeId, setHoveredNodeId] = useState<string | null>(null);

	const { graph, initNodes, initConnections, edgeRelationMap } = useGraphData(useCaseId);

	const { searchTerm, textQuery, nodeId } = Route.useSearch();
	const [visNetwork, setVisNetwork] = useState<any | null>(null);

	const { resolveNodePositions, rememberCreatePosition, startLayout, stopLayout, isLayoutRunning } =
		useNodePositions({
			visNetwork,
			useCaseId,
		});

	const containerRef = useRef<HTMLDivElement>(null);
	const canvasRef = useRef<HTMLCanvasElement>(null);

	useCursorLine({
		visNetwork,
		containerRef,
		canvasRef,
		nodeId,
		createNodeMode,
		allInstances,
	});

	useEffect(() => {
		if (!visNetwork) return;

		const handleHoverNode = ({ node }: { node: string }) => {
			setIsHoveringNode(true);
			setHoveredNodeId(node);
		};
		const handleBlurNode = () => {
			setIsHoveringNode(false);
			setHoveredNodeId(null);
		};

		visNetwork.on('hoverNode', handleHoverNode);
		visNetwork.on('blurNode', handleBlurNode);

		return () => {
			visNetwork.off('hoverNode', handleHoverNode);
			visNetwork.off('blurNode', handleBlurNode);
		};
	}, [visNetwork]);

	useEffect(() => {
		if (!visNetwork) return;

		// vis-network hides edges for any drag, which is what keeps panning a large graph smooth
		// but takes away the very thing a node is dragged for: seeing where it hangs. The event
		// says which of the two is starting, so the option is set to match - and only when it
		// actually differs, because applying options redraws the canvas.
		let hideEdges = true;

		const handleDragStart = ({ nodes }: { nodes: string[] }) => {
			const hide = nodes.length === 0;
			if (hide === hideEdges) return;

			hideEdges = hide;
			visNetwork.setOptions({ interaction: { hideEdgesOnDrag: hide } });
		};

		visNetwork.on('dragStart', handleDragStart);
		return () => visNetwork.off('dragStart', handleDragStart);
	}, [visNetwork]);

	const cursorClass = useMemo(() => {
		if (!createNodeMode) return isHoveringNode ? 'cursor-pointer' : '';
		if (isHoveringNode && hoveredNodeId && nodeId && connectableClassIds) {
			const hovered = allInstances.find(i => i.id === hoveredNodeId);
			if (hovered?.classificationId && !connectableClassIds.has(hovered.classificationId)) {
				return 'cursor-not-allowed';
			}
		}
		return 'cursor-crosshair';
	}, [createNodeMode, isHoveringNode, hoveredNodeId, nodeId, connectableClassIds, allInstances]);

	useGraphSearch({
		visNetwork,
		allInstances,
		useCaseId,
		searchTerm,
		textQuery,
	});

	const { handleClick } = useCreateClickHandler({
		allInstances,
		createNodeMode,
		nodeId,
		onClickGraph,
		connectableClassIds,
	});

	/**
	 * Remembers where an empty spot was clicked while creating, so the new instance is
	 * placed under the cursor.
	 */
	const handleGraphClick = useCallback(
		(event: any) => {
			if (createNodeMode && event?.nodes?.length === 0 && event?.pointer?.canvas) {
				rememberCreatePosition(event.pointer.canvas);
			}
			handleClick(event);
		},
		[createNodeMode, rememberCreatePosition, handleClick]
	);

	const {
		menuState,
		canDelete,
		handleDeleteNode,
		handleDeleteRelation,
		handleContextMenu,
	} = useGraphContextMenu({ allInstances, edgeRelationMap, useCaseId, visNetwork });

	useEffect(() => {
		if (!graphData || !graphData.instances) return;

		setAllInstances(graphData.instances);

		// Nodes carry their position, so adding or removing one leaves the rest of the
		// layout untouched - vis-network does not re-run physics for data changes.
		initNodes(
			graphData.instances,
			resolveNodePositions(graphData.instances),
			connectableClassIds,
			nodeId,
			pendingInstanceIds,
		);

		if (graphData.relations) initConnections(graphData.relations);
	}, [
		graphData,
		createNodeMode,
		initNodes,
		initConnections,
		connectableClassIds,
		nodeId,
		pendingInstanceIds,
		resolveNodePositions,
	]);

	useEffect(() => {
		if (!visNetwork) return;

		const guidelineMap = new Map<string, string>();
		allInstances.forEach(inst => {
			if (inst.id && inst.guidelineName) guidelineMap.set(inst.id, inst.guidelineName);
		});

		// Chips are redrawn on every frame of every pan, zoom and physics step, so the canvas
		// state is set once per frame and each guideline name is measured only once.
		const chipWidths = new Map<string, number>();
		const height = 14;
		const padding = 5;

		const widthOf = (ctx: CanvasRenderingContext2D, name: string) => {
			const cached = chipWidths.get(name);
			if (cached !== undefined) return cached;

			const width = ctx.measureText(name).width + padding * 2;
			chipWidths.set(name, width);
			return width;
		};

		const drawChips = (ctx: CanvasRenderingContext2D) => {
			if (guidelineMap.size === 0) return;
			if (visNetwork.getScale() < MIN_CHIP_SCALE) return;

			// The chips are drawn in graph coordinates, so what is on screen right now follows
			// from inverting the transform vis-network has set up (it only scales and translates).
			// Chips outside of it are skipped: on a large graph the ones off screen are the
			// majority, and each of them costs a rounded rect and a text run per frame.
			const transform = ctx.getTransform();
			const viewLeft = -transform.e / transform.a;
			const viewTop = -transform.f / transform.d;
			const viewRight = viewLeft + ctx.canvas.width / transform.a;
			const viewBottom = viewTop + ctx.canvas.height / transform.d;

			ctx.save();
			ctx.font = 'bold 9px Arial';
			ctx.textAlign = 'center';
			ctx.textBaseline = 'middle';
			ctx.lineWidth = 0.8;
			ctx.strokeStyle = '#1565c0';

			guidelineMap.forEach((guidelineName, nodeId) => {
				const bb = visNetwork.getBoundingBox(nodeId);
				if (!bb) return;

				const top = bb.top - height / 2;
				// The chip sits above the node and is centred on it, so the node's own box is
				// what decides whether it can be seen at all.
				if (bb.right < viewLeft || bb.left > viewRight) return;
				if (top > viewBottom || top + height < viewTop) return;

				const width = widthOf(ctx, guidelineName);
				const cx = (bb.left + bb.right) / 2;

				ctx.beginPath();
				ctx.roundRect(cx - width / 2, top, width, height, height / 2);
				ctx.fillStyle = '#dbeafe';
				ctx.fill();
				ctx.stroke();

				ctx.fillStyle = '#1565c0';
				ctx.fillText(guidelineName, cx, top + height / 2);
			});

			ctx.restore();
		};

		visNetwork.on('afterDrawing', drawChips);
		return () => visNetwork.off('afterDrawing', drawChips);
	}, [visNetwork, allInstances]);


	return (
		<div className="h-full">
			<div
				ref={containerRef}
				id="network-container"
				className={`h-full z-1 relative ${cursorClass}`}
			>
				<canvas
					ref={canvasRef}
					className="absolute top-0 left-0 w-full h-full pointer-events-none z-10"
				/>
				<VisGraph
					graph={graph}
					getNetwork={(network) => {
						setVisNetwork(network);
					}}
					options={graphOptions}
					events={{
						click: handleGraphClick,
						oncontext: handleContextMenu,
					}}
				/>
				{allInstances.length > 0 && (
					<GraphLayoutButton
						isRunning={isLayoutRunning}
						onStart={startLayout}
						onStop={stopLayout}
					/>
				)}
				{menuState && (
					<GraphContextMenu
						menuState={menuState}
						canDelete={canDelete}
						onDeleteNode={handleDeleteNode}
						onDeleteRelation={handleDeleteRelation}
					/>
				)}
			</div>
		</div>
	);
}
