// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import TextField from '@mui/material/TextField';
import { Autocomplete, Box, Chip, createFilterOptions } from '@mui/material';
import { useNavigate, createSearchParams } from 'react-router-dom';
import { useMemo } from 'react';
import { InstanceService_Api_Dto_Instance as Instance } from '../../services/instance';
import { Route } from '../../routes/instancesRoute';
import { useGetGraph } from '../../hooks';

interface TableSearchbarProps {
	/** Instances currently listed in the table, used to offer their classifications. */
	instances: Instance[];
}

/**
 * Picks the classification whose properties the table shows as columns.
 * The choice lives in the URL, so it survives reloads and can be shared.
 */
function TableSearchbar({ instances }: Readonly<TableSearchbarProps>) {
	const navigate = useNavigate();
	const { useCaseId, classificationId, textQuery } = Route.useSearch();

	// Fallback for the initial render, where the table has no rows yet.
	const { data: graph } = useGetGraph(useCaseId);

	const options = useMemo(() => {
		const source = instances.length > 0 ? instances : graph?.instances ?? [];
		return Array.from(
			new Map(source.map(instance => [instance.classificationId, instance])).values()
		).sort((a, b) => (a.classificationName ?? '').localeCompare(b.classificationName ?? ''));
	}, [instances, graph]);

	const selected = useMemo(
		() => options.find(option => option.classificationId === classificationId) ?? null,
		[options, classificationId]
	);

	function selectClassification(newClassificationId?: string | null) {
		const searchParams = {
			useCaseId: `${useCaseId}`,
			...(newClassificationId && { classificationId: newClassificationId }),
			...(textQuery && { textQuery: `${textQuery}` }),
		};

		navigate({
			pathname: Route.path,
			search: `?${createSearchParams(searchParams).toString()}`,
		});
	}

	const filterOptions = createFilterOptions({
		matchFrom: 'any',
		limit: 30,
		stringify: (option: Instance) => option.classificationName ?? '',
	});

	return (
		<Autocomplete
			options={options}
			value={selected}
			size="small"
			sx={{ minWidth: 260 }}
			getOptionLabel={option => option.classificationName ?? ''}
			isOptionEqualToValue={(option, value) => option.classificationId === value.classificationId}
			onChange={(_event, value) => selectClassification(value?.classificationId)}
			filterOptions={filterOptions}
			renderOption={(props, option) => (
				<li {...props} key={option.classificationId}>
					<Box className="flex items-center gap-2">
						<span className="text-sm">{option.classificationName ?? '—'}</span>
						{option.guidelineName && (
							<Chip
								label={option.guidelineName}
								size="small"
								variant="outlined"
								color="primary"
								sx={{ fontSize: '0.68rem', height: 20 }}
							/>
						)}
					</Box>
				</li>
			)}
			renderInput={params => (
				<TextField {...params} label="Show properties of" placeholder="All instances" />
			)}
		/>
	);
}

export default TableSearchbar;
