// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useEffect, useRef, useState } from 'react';
import { Box, Chip, TextField, Typography } from '@mui/material';
import { ClassificationList } from '../../../services/access';
import { InstanceService_Models_Enum_Direction as Direction } from '../../../services/instance';

export interface ClassificationListing {
	id: string;
	name: string;
	direction?: Direction;
	guidelineName?: string | null;
}

interface ClassSearchProps {
	classList: ClassificationListing[];
	setSelectedClassification: (classification: ClassificationList) => void;
}

export default function ClassificationSearch({ classList, setSelectedClassification }: Readonly<ClassSearchProps>) {
	const [search, setSearch] = useState('');
	const [selectedId, setSelectedId] = useState<string | null>(null);
	const inputRef = useRef<HTMLInputElement>(null);

	useEffect(() => {
		inputRef.current?.focus();
	}, []);

	const filtered = classList.filter(c => {
		const q = search.toLowerCase();
		return (
			c.name.toLowerCase().includes(q) ||
			(c.guidelineName ?? '').toLowerCase().includes(q)
		);
	});

	const handleSelect = (item: ClassificationListing) => {
		setSelectedId(item.id);
		setSelectedClassification(item as unknown as ClassificationList);
	};

	return (
		<Box sx={{ display: 'flex', flexDirection: 'column', height: '100%' }}>
			<TextField
				inputRef={inputRef}
				size="small"
				fullWidth
				placeholder="Search classification…"
				value={search}
				onChange={e => setSearch(e.target.value)}
				variant="outlined"
			/>
			<Box
				sx={{
					flex: 1,
					overflowY: 'auto',
					mt: 0.5,
					border: '1px solid',
					borderColor: 'divider',
					borderRadius: 1,
				}}
			>
				{classList.length === 0 && (
					<Box sx={{ p: 1.5 }}>
						<Typography variant="body2" color="text.secondary">
							No compatible classifications are defined for this node type in the ontology.
						</Typography>
					</Box>
				)}
				{classList.length > 0 && filtered.length === 0 && (
					<Box sx={{ p: 1.5 }}>
						<Typography variant="body2" color="text.secondary">
							No results for "{search}"
						</Typography>
					</Box>
				)}
				{filtered.map(c => (
					<Box
						key={c.id}
						onClick={() => handleSelect(c)}
						sx={{
							px: 1.5,
							py: 0.75,
							cursor: 'pointer',
							display: 'flex',
							alignItems: 'center',
							gap: 1,
							bgcolor: selectedId === c.id ? 'action.selected' : 'transparent',
							'&:hover': {
								bgcolor: selectedId === c.id ? 'action.selected' : 'action.hover',
							},
							borderBottom: '1px solid',
							borderColor: 'divider',
							'&:last-child': { borderBottom: 'none' },
						}}
					>
						{c.guidelineName && (
							<Chip
								label={c.guidelineName}
								size="small"
								variant="outlined"
								color="primary"
								sx={{ fontSize: '0.68rem', height: 20, flexShrink: 0 }}
							/>
						)}
						<Typography variant="body2">{c.name}</Typography>
					</Box>
				))}
			</Box>
		</Box>
	);
}
