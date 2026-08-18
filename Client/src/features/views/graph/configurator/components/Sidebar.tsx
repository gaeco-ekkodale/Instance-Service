// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Box, Tabs, Tooltip, Tab } from '@mui/material';
import { SideBarItem } from '../models/SideBarItem';
import { MIcon } from './MIcon';

interface SidebarProps {
	sidebarItems: SideBarItem[];
	currentlySelectedModal: number | boolean;
	setCurrentlySelectedModal: (item: number | boolean) => void;
}

function Sidebar({ sidebarItems, currentlySelectedModal, setCurrentlySelectedModal }: Readonly<SidebarProps>) {
	// Nothing to offer; the bar itself would still be painted.
	if (!sidebarItems.some(item => item.condition)) return null;

	return (
		<Box className="shadow-lg bg-white rounded-md">
			<Tabs
				orientation="vertical"
				selectionFollowsFocus
				value={currentlySelectedModal}
				variant="scrollable"
				indicatorColor="secondary"
				scrollButtons={false}
			>
				{sidebarItems.map((item) => (
					<Tooltip
						key={item.id}
						placement="right"
						title={
							item.description ? (
								<>
									<Box className="font-semibold">{item.name}</Box>
									<Box className="mt-0.5">{item.description}</Box>
								</>
							) : (
								item.name
							)
						}
					>
						<Tab
							icon={MIcon({ name: item.iconName })}
							id={`simple-tab-${item.id}`}
							value={item.id}
							sx={{
								minWidth: '10px',
								display: item.condition ? 'flex' : 'none',
								'&.MuiTab-root': {
									minWidth: '10px',
								},
							}}
							onClick={() => {
								setCurrentlySelectedModal(item.id);
							}}
						/>
					</Tooltip>
				))}
			</Tabs>
		</Box>
	);
}

export default Sidebar;
