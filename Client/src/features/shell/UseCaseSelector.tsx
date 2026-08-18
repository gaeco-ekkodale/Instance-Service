// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Autocomplete, CircularProgress, TextField, Typography, createFilterOptions } from '@mui/material';
import { Route } from '../../routes/instancesRoute';
import { UseCasesService } from '../../services/usecase';
import { UseCase } from '../../services/access';
import { useQuery } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { useNavigate, createSearchParams } from 'react-router-dom';

export default function UseCaseSelector() {
	const {
		data: useCases,
		isLoading,
		isError,
	} = useQuery({
		queryKey: ['usecases'],
		queryFn: () => UseCasesService.getApiUseCases(),
	});
	const [selectedUseCase, setSelectedUseCase] = useState<UseCase | null>(null);

	const navigate = useNavigate();

	const { useCaseId } = Route.useSearch();

	const handleUseCaseChange = (newValue: UseCase | null) => {
		let searchParams = '';
		if(newValue) {
			const params = createSearchParams({useCaseId: `${newValue?.id}`}, );
			searchParams = `?${params.toString()}`;
		}
		navigate({
			pathname: Route.path,
			search: searchParams,
		});
		setSelectedUseCase(newValue);
	};

	useEffect(() => {
		if (useCaseId) {
			const useCase = useCases?.find((useCase) => useCase.id === useCaseId);
			setSelectedUseCase(useCase ?? null);
		}
	}, [useCaseId, useCases]);

	const filterOptions = createFilterOptions({
		matchFrom: 'any',
		stringify: (option: UseCase) => option.name,
	});

	if (isLoading) {
		return <CircularProgress />;
	}

	if (isError) {
		return <Typography color="error">Failed to load use cases.</Typography>;
	}

	return (
		useCases && (
			<Autocomplete
				onChange={(_event, newValue) => {
					handleUseCaseChange(newValue);
				}}
				className="w-full border-red-400"
				sx={{ borderColor: 'red' }}
				value={selectedUseCase}
				options={useCases as unknown as UseCase[]}
				getOptionLabel={(option) => option.name}
				filterOptions={filterOptions}
				renderInput={(params) => <TextField {...params} size="small" label="UseCase" variant="outlined" />}
			/>
		)
	);
}
