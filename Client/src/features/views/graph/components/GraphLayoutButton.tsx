// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box, IconButton, Tooltip } from '@mui/material';
import AutoFixHighIcon from '@mui/icons-material/AutoFixHigh';
import StopIcon from '@mui/icons-material/Stop';

interface GraphLayoutButtonProps {
	isRunning: boolean;
	onStart: () => void;
	onStop: () => void;
}

/**
 * Hands the graph back to the force simulation, which spreads the nodes out again.
 *
 * Offered as a button rather than run on its own: what is on the canvas is the arrangement the
 * user dragged into place, and it is kept between visits, so undoing it is their call. Large
 * graphs take a while to come to rest, which is why it can be stopped halfway.
 */
export function GraphLayoutButton({
	isRunning,
	onStart,
	onStop,
}: Readonly<GraphLayoutButtonProps>) {
	const label = isRunning ? 'Stop arranging' : 'Arrange nodes';
	const title = isRunning
		? 'Stop arranging and leave the nodes where they are'
		: 'Arrange the nodes automatically. This replaces the layout on the canvas.';

	return (
		<Box className="absolute bottom-4 left-4 z-20 rounded-md bg-white shadow-lg">
			<Tooltip title={title} placement="right">
				<IconButton size="small" aria-label={label} onClick={isRunning ? onStop : onStart}>
					{isRunning ? <StopIcon fontSize="small" /> : <AutoFixHighIcon fontSize="small" />}
				</IconButton>
			</Tooltip>
		</Box>
	);
}
