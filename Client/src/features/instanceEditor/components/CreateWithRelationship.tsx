// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

import { Select, MenuItem, FormControl, InputLabel } from '@mui/material';
import {
	InstanceService_Models_Enum_Direction as Direction,
	type InstanceService_Api_Dto_Ontology_RelationDTO as RelationDTO,
} from '../../../services/instance';
import { useEffect, useState } from 'react';
import { useGetRelations } from '../../../hooks';

interface CreateWithRelationshipProps {
	selectedClassificationId: string;
	sourceNodeClassificationId: string;
	setRelation: (label: RelationDTO, direction?: Direction) => void;
}

interface RelationValue {
	direction: Direction;
	predicateId: string;
}

export const CreateWithRelationship = ({ sourceNodeClassificationId, selectedClassificationId, setRelation }: CreateWithRelationshipProps) => {
	const [lrRelations, setLrRelations] = useState<RelationDTO[]>([]);
	const [rlRelations, setRlRelations] = useState<RelationDTO[]>([]);
	const [decodedClassificationId, setDecodedClassificationId] = useState<string>('');
	const [value, setValue] = useState<string>('');

	const { data: possibleRelations } = useGetRelations(selectedClassificationId);

	useEffect(() => {
		setDecodedClassificationId(decodeURIComponent(selectedClassificationId));
	}, [selectedClassificationId]);

	useEffect(() => {
		const lrDirection = possibleRelations?.filter(relation =>
			relation.subjectId === sourceNodeClassificationId &&
			relation.objectId === decodedClassificationId
		);
		const rlDirection = possibleRelations?.filter(relation =>
			relation.subjectId === decodedClassificationId &&
			relation.objectId === sourceNodeClassificationId
		);

		setLrRelations(lrDirection ?? []);
		setRlRelations(rlDirection ?? []);
	}, [possibleRelations, sourceNodeClassificationId, decodedClassificationId]);

	// Auto-select when there is exactly one possible relation
	useEffect(() => {
		const total = lrRelations.length + rlRelations.length;
		if (total !== 1) return;

		if (lrRelations.length === 1) {
			const rel = lrRelations[0];
			const v: RelationValue = { direction: Direction.From, predicateId: rel.predicateId ?? '' };
			setValue(JSON.stringify(v));
			setRelation(rel, Direction.From);
		} else {
			const rel = rlRelations[0];
			const v: RelationValue = { direction: Direction.To, predicateId: rel.predicateId ?? '' };
			setValue(JSON.stringify(v));
			setRelation(rel, Direction.To);
		}
	// eslint-disable-next-line react-hooks/exhaustive-deps
	}, [lrRelations, rlRelations]);

	const SplitRelationUrl = (relationUrl?: string | null) => {
		return relationUrl?.split('/').pop() ?? '';
	};

	return (
		<FormControl className='w-full' size="small">
			<InputLabel size="small">Connection</InputLabel>
			<Select
				labelId="Connection Type"
				className="select_parent"
				label="ConnectionType"
				size="small"
				value={value || ''}
				onChange={(e) => {
					const selectedValue = e.target.value;
					setValue(selectedValue);

					if (selectedValue !== '') {

						try {
							const relationValue: RelationValue = JSON.parse(selectedValue);

							if (relationValue.direction === Direction.From) {
								const selectedRelation = lrRelations?.find((relation) =>
									relation.predicateId === relationValue.predicateId
									&& relation.subjectId === sourceNodeClassificationId
									&& relation.objectId === decodedClassificationId
								);
								if (selectedRelation) setRelation(selectedRelation, relationValue.direction);
							} else if (relationValue.direction === Direction.To) {
								const selectedRelation = rlRelations?.find((relation) =>
									relation.predicateId === relationValue.predicateId
									&& relation.objectId === sourceNodeClassificationId
									&& relation.subjectId === decodedClassificationId
								);
								if (selectedRelation) setRelation(selectedRelation, relationValue.direction);
							}
						} catch (error) {
							console.log("Parsing values of MenuItems failed");
						}
					}
				}}
			>
				{lrRelations?.map((relation) => {
					const displayText = `(${SplitRelationUrl(relation.subjectId)})-[${relation.label || SplitRelationUrl(relation.predicateId)}]->(${SplitRelationUrl(relation.objectId)})`;
					const value: RelationValue = { direction: Direction.From, predicateId: relation.predicateId ?? '' };
					return (
						<MenuItem
							key={`lr-${relation.predicateId}`}
							value={JSON.stringify(value)}
						>
							{displayText}
						</MenuItem>
					);
				})}
				{rlRelations?.map((relation) => {
					const displayText = `(${SplitRelationUrl(relation.objectId)})<-[${relation.label || SplitRelationUrl(relation.predicateId)}]-(${SplitRelationUrl(relation.subjectId)})`;
					const value: RelationValue = { direction: Direction.To, predicateId: relation.predicateId ?? '' };
					return (
						<MenuItem
							key={`rl-${relation.predicateId}`}
							value={JSON.stringify(value)}
						>
							{displayText}
						</MenuItem>
					);
				})}
			</Select>
		</FormControl>
	);
};
