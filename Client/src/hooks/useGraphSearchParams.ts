// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useSearchParams } from 'react-router-dom';

export interface GraphSearchParams {
    useCaseId: string | null;
    nodeId: string | null;
    /** Instance id the graph view focuses on. */
    searchTerm: string | null;
    /** Classification whose properties the table view shows as columns. */
    classificationId: string | null;
    textQuery: string | null;
}

/**
 * Hook to extract graph-related search parameters from the URL.
 * Used across multiple components that need access to URL state.
 */
export const useGraphSearchParams = (): GraphSearchParams => {
    const [searchParams] = useSearchParams();

    return {
        useCaseId: searchParams.get('useCaseId'),
        nodeId: searchParams.get('nodeId'),
        searchTerm: searchParams.get('searchTerm'),
        classificationId: searchParams.get('classificationId'),
        textQuery: searchParams.get('textQuery'),
    };
};
