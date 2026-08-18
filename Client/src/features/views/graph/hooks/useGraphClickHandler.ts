// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useCallback } from 'react';
import { InstanceService_Api_Dto_Instance as Instance } from '../../../../services/instance';

export interface UseGraphClickHandlerOptions {
    useCaseId: string | null;
    createNodeMode: boolean;
    setTargetNodeId: (id: string | undefined | null) => void;
    navigateToNode: (nodeId?: string) => void;
}

export const useGraphClickHandler = ({
    useCaseId,
    createNodeMode,
    setTargetNodeId,
    navigateToNode,
}: UseGraphClickHandlerOptions) => {
    const handleGraphClick = useCallback(
        (node?: Instance) => {
            if (!useCaseId) return;

            const currentNodeId = new URLSearchParams(window.location.search).get('nodeId');

            // Handle create node mode with existing source node
            if (createNodeMode && currentNodeId) {
                if (node) {
                    // Clicked on another node - set as target
                    setTargetNodeId(node.id!);
                } else {
                    // Clicked on empty space - allow new node creation
                    setTargetNodeId(null);
                }
                return;
            }

            // Handle create node mode without source node
            if (createNodeMode && !currentNodeId && !node) {
                setTargetNodeId(null);
                return;
            }

            // Normal navigation mode
            navigateToNode(node?.id ?? undefined);
        },
        [useCaseId, createNodeMode, setTargetNodeId, navigateToNode]
    );

    return { handleGraphClick };
};
