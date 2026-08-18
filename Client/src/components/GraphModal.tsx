// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Typography, Divider, Modal, Box } from '@mui/material';

/**
 * GraphModal component for displaying modal dialogs with a title and custom content.
 *
 * @param modalTitle - The title text displayed at the top of the modal.
 * @param children - The content to be rendered inside the modal.
 * @param className - Optional. Additional CSS classes for styling the modal container.
 * @param open - Controls whether the modal is visible.
 * @param onClose - Optional. Callback invoked when the modal requests to be closed.
 */
interface GraphModalProps {
	modalTitle: string;
	children: React.ReactNode;
	className?: string;
	open: boolean;
	onClose?: () => void;
}

/**
 * Renders a modal window with a title and custom children.
 * Leverages @mui/material's Modal, Box, Typography, and Divider components.
 */
export const GraphModal = ({
	modalTitle,
	children,
	className = "",
	open,
	onClose,
}: GraphModalProps) => {
	return (
		<Modal
		open={open}
		onClose={onClose}
		aria-labelledby="modal-title"
		aria-describedby="modal-description"
		>
			<Box
				className={`fixed left-[100px] top-1/2 -translate-y-1/2 max-h-[90vh] flex flex-col overflow-hidden bg-white shadow-lg p-4 rounded-md ${className}`}
				sx={{ outline: 0 }}
			>
				<div className="shrink-0 mb-2">
					<Typography id="modal-title" variant="h6" noWrap>
						{modalTitle}
					</Typography>
					<Divider />
				</div>

				<div className="flex flex-col min-h-0 flex-1">{children}</div>
			</Box>
		</Modal>
	);
};
