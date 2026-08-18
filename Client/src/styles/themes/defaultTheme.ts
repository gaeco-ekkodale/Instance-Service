// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { createTheme } from '@mui/material';

interface ThemeColors {
	primaryColor: string;
	secondaryColor: string;
	backgroundColor: string;
	primaryColorDark: string;
	secondaryColorLight: string;
	primaryColorLight: string;
}

export const Themecolors: ThemeColors = {
	backgroundColor: '#FFFFFF',
	primaryColor: '#001A4A',
	secondaryColorLight: '#03ffd4',
	secondaryColor: '#00ecc4',
	primaryColorDark: '#ff9900',
	primaryColorLight: '#518eff',
};

const baseTheme = {
	palette: {
	  action: {
		disabled: Themecolors.primaryColor,
	  },
	  primary: {
		main: Themecolors.primaryColor,
		light: Themecolors.primaryColorLight,
	  },
	  secondary: {
		main: Themecolors.secondaryColor,
		light: Themecolors.secondaryColorLight,
	  },
	  error: {
		main: '#f44336', // Standard red for error
	  },
	  warning: {
		main: '#ffa726', // Standard orange for warning
	  },
	  background: {
		default: '#FFFFFF',
	  },
	  text: {
		primary: Themecolors.primaryColor,
		disabled: Themecolors.primaryColor,
	  },
	  body: { backgroundColor: '#313131' },
	},
	typography: {
	  allVariants: {
		color: Themecolors.primaryColor,
	  },
	  h5: {
		color: Themecolors.primaryColor,
	  },
	},
  };
  
  export const defaultTheme = createTheme({
	components: {
		MuiButton: {
			styleOverrides: {
			  root: {
				'&.MuiButton-containedPrimary': {
				  backgroundColor: Themecolors.primaryColor,
				  '&:hover': {
					backgroundColor: Themecolors.primaryColorDark,
				  },
				},
				'&.MuiButton-containedSecondary': {
				  backgroundColor: Themecolors.secondaryColor,
				  '&:hover': {
					backgroundColor: Themecolors.secondaryColorLight,
				  },
				},
			  },
			},
		  },		  
	  MuiTooltip: {
		styleOverrides: {
		  tooltip: {
			backgroundColor: '#FFFFFF',
			fontSize: 15,
			color: Themecolors.primaryColor,
			boxShadow: '0 0 10px rgba(0, 0, 0, 0.2)',
		  },
		},
	  },
	  MuiTabs: {
		styleOverrides: {
		  root: {
			'&.edit-tabs': { height: '5vh' },
		  },
		},
	  },
	  MuiIcon: {
		styleOverrides: {
		  colorPrimary: '#FFFFFF',
		  root: {
			color: '#FFFFFF',
		  },
		},
	  },
	  MuiSvgIcon: {
		styleOverrides: {
		  root: {
			'&.delete-icon': { color: '#FFFFFF !important' },
		  },
		},
	  },
	},
	...baseTheme,
  });  

export const MuiFormThemeEnabled = createTheme({
	...baseTheme,
});
