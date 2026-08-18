// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useState } from 'react';
import { TextField, FormControlLabel, Checkbox, FormControl, InputLabel, Select, MenuItem } from '@mui/material';
import {
  type InstanceService_Api_Dto_MetadataProperty,
  Guideline_Model_Enums_StorageType,
} from '../../../services/instance';
import LockOutlineIcon from '@mui/icons-material/LockOutlined';

interface PropertyInputFieldProps {
  property: InstanceService_Api_Dto_MetadataProperty;
  value: any;
  onChange: (propertyName: string, value: any) => void;
}

const textPlaceholder: Partial<Record<Guideline_Model_Enums_StorageType, string>> = {
  [Guideline_Model_Enums_StorageType.String]: 'Enter text…',
  [Guideline_Model_Enums_StorageType.Character]: 'Single character…',
  [Guideline_Model_Enums_StorageType.Integer]: '0',
  [Guideline_Model_Enums_StorageType.Real]: '0.00',
};

export const PropertyInputField = ({ property, value, onChange }: PropertyInputFieldProps) => {
  const { name, storageType, isReadOnly, propertyType, enumValues, min, max } = property;

  // Used to switch date/time inputs between 'text' (shows placeholder) and native type (shows picker).
  const [dateFocused, setDateFocused] = useState(false);

  if (!name || !storageType) return null;

  if (isReadOnly) {
    return (
      <TextField
        size="small"
        margin="dense"
        disabled
        label={
          <span className="flex items-center">
            <LockOutlineIcon sx={{ fontSize: 14, mr: 0.5 }} />
            {name}
          </span>
        }
        value={value ?? ''}
        InputProps={{ readOnly: true }}
        InputLabelProps={{ shrink: true }}
        fullWidth
      />
    );
  }

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    let newValue: any;
    switch (storageType) {
      case Guideline_Model_Enums_StorageType.Boolean:
        // Store as string so "false" is never dropped by falsy checks in the data pipeline.
        newValue = String(e.target.checked);
        break;
      case Guideline_Model_Enums_StorageType.Integer:
        newValue = parseInt(e.target.value, 10);
        break;
      case Guideline_Model_Enums_StorageType.Real:
        newValue = parseFloat(e.target.value);
        break;
      case Guideline_Model_Enums_StorageType.Character:
        newValue = e.target.value.slice(0, 1);
        break;
      default:
        newValue = e.target.value;
    }
    onChange(name, newValue);
  };

  if (propertyType === 'PropertyEnum' && enumValues?.length) {
    return (
      <FormControl size="small" margin="dense" fullWidth>
        <InputLabel shrink>{name}</InputLabel>
        <Select
          label={name}
          notched
          displayEmpty
          value={value ?? ''}
          onChange={(e) => onChange(name, e.target.value)}
          renderValue={(v) =>
            v ? <span>{v as string}</span> : <span style={{ color: '#9e9e9e' }}>Select option…</span>
          }
        >
          <MenuItem value=""><em>—</em></MenuItem>
          {enumValues.map((ev) => (
            <MenuItem key={ev.id ?? ev.name} value={ev.name ?? ''}>{ev.name}</MenuItem>
          ))}
        </Select>
      </FormControl>
    );
  }

  switch (storageType) {
    case Guideline_Model_Enums_StorageType.Boolean:
      return (
        <FormControlLabel
          sx={{ my: 0.5 }}
          control={
            <Checkbox
              size="small"
              checked={value === true || value === 'true'}
              onChange={handleChange}
              color="primary"
            />
          }
          label={name}
        />
      );
    case Guideline_Model_Enums_StorageType.Integer:
    case Guideline_Model_Enums_StorageType.Real:
      return (
        <TextField
          size="small"
          margin="dense"
          label={name}
          type="number"
          placeholder={textPlaceholder[storageType]}
          inputProps={{
            step: storageType === Guideline_Model_Enums_StorageType.Real ? 'any' : 1,
            ...(min != null && { min }),
            ...(max != null && { max }),
          }}
          InputLabelProps={{ shrink: true }}
          value={value ?? ''}
          onChange={handleChange}
          fullWidth
        />
      );
    case Guideline_Model_Enums_StorageType.String:
    case Guideline_Model_Enums_StorageType.Character:
      return (
        <TextField
          size="small"
          margin="dense"
          label={name}
          placeholder={textPlaceholder[storageType]}
          inputProps={{ maxLength: storageType === Guideline_Model_Enums_StorageType.Character ? 1 : undefined }}
          InputLabelProps={{ shrink: true }}
          value={value ?? ''}
          onChange={handleChange}
          fullWidth
        />
      );
    case Guideline_Model_Enums_StorageType.Date:
    case Guideline_Model_Enums_StorageType.Time: {
      const isDate = storageType === Guideline_Model_Enums_StorageType.Date;
      // Show native date/time picker only when focused or when a value is already set.
      // Otherwise show as text with a greyed placeholder so the field looks clearly empty.
      const resolvedType = dateFocused || value ? (isDate ? 'date' : 'time') : 'text';
      return (
        <TextField
          size="small"
          margin="dense"
          label={name}
          type={resolvedType}
          placeholder={resolvedType === 'text' ? (isDate ? 'DD.MM.YYYY' : 'HH:MM') : undefined}
          InputLabelProps={{ shrink: true }}
          value={value ?? ''}
          onChange={handleChange}
          onFocus={() => setDateFocused(true)}
          onBlur={() => setDateFocused(false)}
          fullWidth
        />
      );
    }
    default:
      return null;
  }
};
