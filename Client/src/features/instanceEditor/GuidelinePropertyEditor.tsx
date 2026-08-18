// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { useState, useEffect, useMemo, useDeferredValue } from 'react';
import { Typography, Box, TextField, Accordion, AccordionSummary, AccordionDetails, Chip, InputAdornment, LinearProgress, Skeleton } from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import SearchIcon from '@mui/icons-material/Search';
import { PropertyInputField } from './components/PropertyInputField';
import { useGetClassification } from '../../hooks';
import { pendingSx } from '../instances/pendingSx';
import {
  type InstanceService_Api_Dto_MetadataProperty,
  Guideline_Model_Enums_StorageType,
} from '../../services/instance';

interface GuidelinePropertyEditorProps {
  classificationId: string;
  currentValues?: Record<string, any>;
  useCaseId: string;
  onPropertiesChange: (properties: Record<string, any>) => void;
  metadataProperties?: InstanceService_Api_Dto_MetadataProperty[];
  /** Names of the properties whose value is not saved yet, marked as in the tables. */
  pendingProperties?: Set<string>;
}

const isFilled = (v: any) => v !== undefined && v !== '' && v !== null;

// Booleans are always in a valid state (true or false) — exclude from fill counter.
const isCounted = (p: InstanceService_Api_Dto_MetadataProperty) =>
  p.storageType !== Guideline_Model_Enums_StorageType.Boolean;

// Enums and trees always span full width; simple types can share a column.
const isFullWidth = (p: InstanceService_Api_Dto_MetadataProperty) =>
  p.propertyType === 'PropertyEnum' ||
  p.propertyType === 'PropertySuperEnum' ||
  p.propertyType === 'PropertyTree' ||
  p.storageType === Guideline_Model_Enums_StorageType.Boolean;

export default function GuidelinePropertyEditor({
  classificationId,
  useCaseId,
  currentValues,
  onPropertiesChange,
  metadataProperties,
  pendingProperties,
}: Readonly<GuidelinePropertyEditorProps>) {
  const [newProperties, setNewProperties] = useState<Record<string, any>>({});
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');

  // Debounce search by 250ms
  useEffect(() => {
    const t = setTimeout(() => setSearch(searchInput), 250);
    return () => clearTimeout(t);
  }, [searchInput]);

  const {
    data: classification,
    isLoading,
    error,
  } = useGetClassification(
    metadataProperties ? undefined : useCaseId,
    metadataProperties ? undefined : classificationId,
  );

  useEffect(() => {
    if (currentValues) {
      setNewProperties(currentValues);
    }
  }, [currentValues]);

  const handlePropertyChange = (propertyName: string, value: any) => {
    const updatedProperties = { ...newProperties, [propertyName]: value };
    setNewProperties(updatedProperties);
    onPropertiesChange(updatedProperties);
  };

  // Build the flat property list once when classification data arrives.
  // useMemo stabilises the reference so useDeferredValue can detect "new data".
  const allProperties = useMemo<InstanceService_Api_Dto_MetadataProperty[]>(() => {
    if (metadataProperties) return metadataProperties;
    const props: InstanceService_Api_Dto_MetadataProperty[] = [];
    classification?.propertySets?.forEach((propertySet) => {
      propertySet.properties?.forEach((property) => {
        if (property.right === 'None') return;
        props.push({
          id: property.id,
          name: property.name,
          storageType: property.storageType as unknown as Guideline_Model_Enums_StorageType,
          propertySetName: propertySet.name ?? undefined,
          isReadOnly: property.right !== 'Write',
          propertyType: property.propertyType ?? 'PropertySimple',
          enumValues: property.enumValues ?? [],
        });
      });
    });
    return props;
  }, [classification, metadataProperties]);

  // Defer rendering the accordion list so the UI stays responsive while React
  // works through the (potentially large) set of PropertyInputField components.
  const deferredProperties = useDeferredValue(allProperties);
  const isTransitioning = deferredProperties !== allProperties;

  const matchesSearch = (p: InstanceService_Api_Dto_MetadataProperty) =>
    !search || p.name?.toLowerCase().includes(search.toLowerCase());

  const renderAccordions = (properties: InstanceService_Api_Dto_MetadataProperty[]) => {
    const filtered = properties.filter(matchesSearch);

    const grouped = filtered.reduce<Record<string, InstanceService_Api_Dto_MetadataProperty[]>>(
      (acc, prop) => {
        const key = prop.propertySetName ?? '';
        if (!acc[key]) acc[key] = [];
        acc[key].push(prop);
        return acc;
      },
      {}
    );

    if (filtered.length === 0) {
      return (
        <Typography variant="body2" color="text.secondary" sx={{ p: 2, textAlign: 'center' }}>
          No properties match "{search}"
        </Typography>
      );
    }

    return Object.entries(grouped).sort(([a], [b]) => a.localeCompare(b)).map(([setName, props]) => {
      const countable = props.filter(isCounted);
      const filledCount = countable.filter(p => isFilled(newProperties[p.name!])).length;
      const complete = countable.length > 0 && filledCount === countable.length;
      // Sets are collapsed, so the mark of a pending property has to reach the header.
      const hasPending = props.some(p => pendingProperties?.has(p.name!));

      return (
        <Accordion
          key={setName}
          defaultExpanded={false}
          disableGutters
          elevation={0}
          TransitionProps={{ unmountOnExit: true }}
          sx={{ '&:before': { display: 'none' }, border: '1px solid', borderColor: 'divider', mb: 0.5, borderRadius: '4px !important' }}
        >
          <AccordionSummary
            expandIcon={<ExpandMoreIcon />}
            sx={{
              minHeight: 36,
              px: 1.5,
              '& .MuiAccordionSummary-content': { my: 0.5, alignItems: 'center', gap: 1 },
              ...(hasPending && pendingSx),
            }}
          >
            <Typography variant="caption" fontWeight={600} color="text.primary" sx={{ flexGrow: 1 }}>
              {setName || 'General'}
            </Typography>
            <Chip
              label={`${filledCount} / ${countable.length}`}
              size="small"
              color={complete ? 'success' : 'default'}
              sx={{ fontSize: '0.65rem', height: 18, '& .MuiChip-label': { px: 0.75 } }}
            />
          </AccordionSummary>
          <AccordionDetails sx={{ pt: 0.5, pb: 1, px: 1.5 }}>
            <Box sx={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 0.5 }}>
              {[...props].sort((a, b) => (a.name ?? '').localeCompare(b.name ?? '')).map((property) => (
                <Box
                  key={property.name}
                  sx={{
                    gridColumn: isFullWidth(property) ? 'span 2' : 'span 1',
                    p: 0.5,
                    ...(pendingProperties?.has(property.name!) && pendingSx),
                  }}
                >
                  <PropertyInputField
                    property={property}
                    value={newProperties[property.name!]}
                    onChange={handlePropertyChange}
                  />
                </Box>
              ))}
            </Box>
          </AccordionDetails>
        </Accordion>
      );
    });
  };

  const renderSearchBox = (total: number, filled: number) => (
    <Box className="shrink-0 pb-2" sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
      <TextField
        size="small"
        placeholder="Search properties…"
        value={searchInput}
        onChange={(e) => setSearchInput(e.target.value)}
        sx={{ flexGrow: 1 }}
        InputProps={{
          startAdornment: (
            <InputAdornment position="start">
              <SearchIcon fontSize="small" color="action" />
            </InputAdornment>
          ),
        }}
      />
      <Typography variant="caption" color={filled === total ? 'success.main' : 'text.secondary'} sx={{ whiteSpace: 'nowrap' }}>
        {filled} / {total} filled
      </Typography>
    </Box>
  );

  if (isLoading || isTransitioning) return (
    <Box sx={{ px: 1, pt: 1 }}>
      <LinearProgress sx={{ mb: 1.5, borderRadius: 1 }} />
      {[...Array(6)].map((_, i) => (
        <Skeleton key={i} variant="rounded" height={36} sx={{ mb: 0.5 }} />
      ))}
    </Box>
  );
  if (error) return <Typography>Error loading classification data.</Typography>;

  const countableAll = deferredProperties.filter(isCounted);
  const totalFilled = countableAll.filter(p => isFilled(newProperties[p.name!])).length;

  return (
    <Box className="flex flex-col min-h-0 flex-1">
      {renderSearchBox(countableAll.length, totalFilled)}
      <Box className="flex-1 min-h-0 overflow-y-auto" sx={{ pb: 4 }}>
        {renderAccordions(deferredProperties)}
      </Box>
    </Box>
  );
}
