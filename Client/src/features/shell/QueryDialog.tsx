// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import {
	Button,
	Dialog,
	DialogActions,
	DialogContent,
	DialogTitle,
	TextField,
	Typography,
} from '@mui/material';
import SendIcon from '@mui/icons-material/Send';
import DeleteIcon from '@mui/icons-material/Delete';
import { createSearchParams, useNavigate } from 'react-router-dom';
import { useEffect, useState } from 'react';
import { Route } from '../../routes/instancesRoute';

interface QueryDialogProps {
	open: boolean;
	onClose: () => void;
}

/**
 * Writes a Cypher query for the graph and the table.
 *
 * A dialog rather than a field in the header: queries run over several lines, and the
 * input can be dragged as large as needed without the header having to grow with it.
 */
export default function QueryDialog({ open, onClose }: Readonly<QueryDialogProps>) {
	const navigate = useNavigate();
	const { useCaseId, textQuery } = Route.useSearch();

	const [query, setQuery] = useState<string>('');

	useEffect(() => {
		if (textQuery) setQuery(decodeURIComponent(textQuery));
	}, [textQuery]);

	/** The query lives in the URL, so it survives reloads and can be shared. */
	function applyQuery(nextQuery?: string) {
		if (!useCaseId) return;

		const searchParams = {
			useCaseId: `${useCaseId}`,
			...(nextQuery && { textQuery: `${encodeURIComponent(nextQuery)}` }),
		};

		navigate({
			pathname: Route.path,
			search: `?${createSearchParams(searchParams).toString()}`,
		});
		onClose();
	}

	return (
		<Dialog open={open} onClose={onClose} fullWidth maxWidth="md">
			<DialogTitle>Cypher query</DialogTitle>
			<DialogContent>
				<Typography variant="caption" color="text.secondary">
					Filters the instances of the use case in both the graph and the table.
				</Typography>
				<TextField
					autoFocus
					fullWidth
					multiline
					minRows={8}
					placeholder="MATCH (n) RETURN n"
					value={query}
					onChange={event => setQuery(event.currentTarget.value)}
					sx={{
						mt: 1,
						'& .MuiInputBase-input': { fontFamily: 'monospace', resize: 'vertical' },
					}}
				/>
			</DialogContent>
			<DialogActions>
				<Button
					color="error"
					startIcon={<DeleteIcon />}
					onClick={() => {
						setQuery('');
						applyQuery(undefined);
					}}
				>
					Clear
				</Button>
				<Button onClick={onClose} sx={{ ml: 'auto' }}>
					Cancel
				</Button>
				<Button variant="contained" endIcon={<SendIcon />} onClick={() => applyQuery(query)}>
					Use query
				</Button>
			</DialogActions>
		</Dialog>
	);
}
