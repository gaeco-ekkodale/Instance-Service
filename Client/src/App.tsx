// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import './index.css';
import { ThemeProvider } from '@emotion/react';
import { CssBaseline } from '@mui/material';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { SnackbarProvider } from 'notistack';
import { Suspense, useEffect } from 'react';
import { useAuth } from 'react-oidc-context';
import { Route, Routes } from 'react-router-dom';
import { Toaster } from 'sonner';
import LoadingSpinner from './components/feedback/LoadingSpinner';
import InstancesPage from './pages/InstancesPage';
import { OpenAPI as AccessAPI } from './services/access/core/OpenAPI';
import { OpenAPI as GuidelineAPI } from './services/guideline/core/OpenAPI';
import { OpenAPI as InstanceAPI } from './services/instance/core/OpenAPI';
import { OpenAPI as OntologyAPI } from './services/ontology/core/OpenAPI';
import { OpenAPI as UsecaseAPI } from './services/usecase/core/OpenAPI';
import { defaultTheme } from './styles/themes/defaultTheme';

// Query Client setzen
const queryClient = new QueryClient();

// API-Basis-URL setzen
AccessAPI.BASE = import.meta.env.VITE_ACCESS_SERVER_URL;
UsecaseAPI.BASE = import.meta.env.VITE_USECASE_SERVER_URL;
OntologyAPI.BASE = import.meta.env.VITE_ONTOLOGY_SERVER_URL;
GuidelineAPI.BASE = import.meta.env.VITE_GUIDELINE_SERVER_URL;
InstanceAPI.BASE = import.meta.env.VITE_INSTANCE_SERVER_URL;

function App() {
	const auth = useAuth();
	const setTokens = (value: string | undefined) => {
		AccessAPI.TOKEN = value;
		UsecaseAPI.TOKEN = value;
		OntologyAPI.TOKEN = value;
		GuidelineAPI.TOKEN = value;
		InstanceAPI.TOKEN = value;
	};
	setTokens(auth.user?.access_token);

	useEffect(() => {
		if (auth.user?.access_token) {
			setTokens(auth.user?.access_token);
		} else {
			setTokens(undefined);
		}
	}, [auth]);

	return (
		<div>
			<Routes>
				<Route
					path="/*"
					element={
						<ThemeProvider theme={defaultTheme}>
							<CssBaseline />
							<Toaster richColors />
							<QueryClientProvider client={queryClient}>
								<SnackbarProvider maxSnack={3}>
									<Suspense fallback={<LoadingSpinner fullscreen={true} />}>
										<InstancesPage />
									</Suspense>
								</SnackbarProvider>
							</QueryClientProvider>
						</ThemeProvider>
					}
				/>
			</Routes>
		</div>
	);
}

export default App;
