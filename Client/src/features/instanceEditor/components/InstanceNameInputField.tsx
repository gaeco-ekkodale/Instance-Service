// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { TextField } from "@mui/material";
import LockOutlineIcon from '@mui/icons-material/LockOutlined';

interface NameInputFieldProps {
    label: string;
    value: string;
    isReadonly: boolean;
    onChange: (newName: string) => void;
}

export const InstanceNameInputField = ({label, value, isReadonly, onChange}: NameInputFieldProps) =>{
    return (
        <TextField
            size="small"
            margin="dense"
            disabled={isReadonly}
            label={
                <span className="flex items-center">
                    {isReadonly && <LockOutlineIcon sx={{ fontSize: 14, mr: 0.5 }} />}
                    {label}
                </span>
            }
            value={value || ''}
            onChange={(e) => onChange(e.target.value)}
            InputLabelProps={{ shrink: isReadonly ? true : undefined }}
            fullWidth
        />
    )
}
