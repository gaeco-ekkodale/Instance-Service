// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import Box from '@mui/material/Box/Box';
import CircularProgress from '@mui/material/CircularProgress/CircularProgress';

interface Props {
	fullscreen: boolean;
	overlay?: boolean;
}

/**
 * LoadingSpinner Component
 *
 * This component is a simple loading spinner used to indicate that a process is ongoing.
 * It displays a centered circular progress indicator. This component is typically used
 * when data is being fetched or a page is loading, providing a visual cue to the user that
 * an operation is in progress.
 *
 * The component uses Material-UI components and styles to create a full-height container
 * with a centered spinner.
 *
 * @param fullscreen - Whether the spinner should take up the full screen height
 * @param overlay - Whether the spinner should be positioned absolutely on top of content (does not block interaction)
 * @returns {JSX.Element} - A flexbox container that centers the CircularProgress spinner.
 */
const LoadingSpinner = ({ fullscreen, overlay }: Props): JSX.Element => {
	const boxStyle = {
		display: 'flex',
		alignItems: 'center',
		justifyContent: 'center',
		height: fullscreen ? 'calc(100vh - 4rem)' : 'auto',
		width: fullscreen ? '100vw' : 'auto',
		...(overlay && {
			position: 'absolute' as const,
			top: 0,
			left: 0,
			right: 0,
			bottom: 0,
			pointerEvents: 'none' as const,
		}),
	};

	return (
		<Box sx={boxStyle}>
			<CircularProgress />
		</Box>
	);
};
export default LoadingSpinner;

