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
import { useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import {
	InstanceService_Api_Dto_Graph as Graph,
	InstanceService_Api_Dto_Instance as Instance,
} from '../../services/instance';
import { Route } from '../../routes/instancesRoute';

function Searchbar() {
	const navigate = useNavigate();
	const [instances, setInstances] = useState<Instance[]>([]);
	const [selectedInstance, setSelectedInstances] = useState<Instance | null>(null);

	const queryClient = useQueryClient();

	const {useCaseId, nodeId, searchTerm, textQuery} = Route.useSearch();

	useEffect(() => {
		let graph: Graph | undefined;
		if(textQuery) {
			graph = queryClient.getQueryData<Graph>(['filteredNodesGraph', useCaseId, textQuery]);
		} else {
			graph = queryClient.getQueryData<Graph>(['nodesGraph', useCaseId]);
		}
		if (graph?.instances) {
			setInstances(graph.instances);
		}
	}, [useCaseId, textQuery]);

	function handleSearch(searchValue: string) {
		if(!useCaseId) return;
		const searchParams = {
			useCaseId: `${useCaseId}`,
			searchTerm: `${searchValue}`,
			...(textQuery && { textQuery: `${textQuery}` })
		};
		const params = createSearchParams(searchParams);

		navigate({
			pathname: Route.path,
			search: `?${params.toString()}`,
		});
	}

	const filterOptions = createFilterOptions({
		matchFrom: 'any',
		stringify: (option: Instance) => option.name ?? '',
	});

	const onClose = () => {
		if(!useCaseId || !nodeId) return;
		const searchParams = {
			useCaseId: `${useCaseId}`,
			nodeId: `${nodeId}`,
			...(textQuery && { textQuery: `${textQuery}` })
		};
		const params = createSearchParams(searchParams);

		navigate({
			pathname: Route.path,
			search: `?${params.toString()}`,
		});
	};

	useEffect(() => {
		if (searchTerm) {
			const instance = instances?.find((instance) => instance.name === searchTerm);
			setSelectedInstances(instance ?? null);
		} else {
			setSelectedInstances(null);
			// TODO: should we close the dialog here?
		}
	}, [searchTerm, instances]);

	return (
		<Autocomplete
			className="w-64"
			options={instances}
			getOptionLabel={(option) => option.name ?? ''} // Automatically sets the key to the name of the instance
			// Set the key to the id of the instance, names are not unique
			renderOption={(props, option) => (
				<li {...props} key={option.id}>
					<Box className="flex items-center gap-2">
						<span>{option.name}</span>
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
			onBlur={() => onClose()}
			value={selectedInstance}
			onChange={(_event, value) => {
				if(value?.id) {
					handleSearch(value.id);
				}
			}}
			filterOptions={filterOptions}
			renderInput={(params) => (
				<TextField
					{...params}
					value={searchTerm}
					size="small"
					InputProps={{
						...params.InputProps,
						className: 'search-text',
					}}
				/>
			)}
		/>
	);
}

export default Searchbar;
