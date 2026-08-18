// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import * as MUIcon from '@mui/icons-material';
import { IconTypeMap } from '@mui/material';

type IconProps = IconTypeMap['props'];

interface MIconProps extends IconProps {
	name: string; // The name of the Material-UI icon to be rendered
}

/**
 * MIcon is a utility function that dynamically renders Material-UI icons based on the provided name.
 * @param {MIconProps} props - The props for the MIcon component.
 * @returns {JSX.Element} - The rendered Material-UI icon.
 */
export function MIcon(props: MIconProps): JSX.Element {
	const { name } = props;

	// Retrieve the corresponding Material-UI icon component based on the provided name
	const Icon = MUIcon[name as keyof typeof MUIcon];

	// If the specified icon does not exist, throw an error
	if (Icon == null) {
		throw new Error(`There is no "${name}" Icon`);
	}

	// Render the Material-UI icon with the provided props
	return <Icon {...props} />;
}
