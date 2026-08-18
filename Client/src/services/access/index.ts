// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */
export { ApiError } from './core/ApiError';
export { CancelablePromise, CancelError } from './core/CancelablePromise';
export { OpenAPI } from './core/OpenAPI';
export type { OpenAPIConfig } from './core/OpenAPI';

export type { AccessRightDTO } from './models/AccessRightDTO';
export type { BadRequestResult } from './models/BadRequestResult';
export type { Classification } from './models/Classification';
export type { ClassificationList } from './models/ClassificationList';
export type { ClassificationPropertyDTO } from './models/ClassificationPropertyDTO';
export { ClassificationRight } from './models/ClassificationRight';
export type { ClassificationsListSet } from './models/ClassificationsListSet';
export type { NotFoundResult } from './models/NotFoundResult';
export type { ProblemDetails } from './models/ProblemDetails';
export type { Property } from './models/Property';
export { PropertyRight } from './models/PropertyRight';
export type { PropertySet } from './models/PropertySet';
export { PropertySetRight } from './models/PropertySetRight';
export { StorageType } from './models/StorageType';
export type { UseCase } from './models/UseCase';
export type { UserGroupDTO } from './models/UserGroupDTO';

export { AccessRightsService } from './services/AccessRightsService';
export { ClassificationsService } from './services/ClassificationsService';
export { UseCasesService } from './services/UseCasesService';
export { UserGroupsService } from './services/UserGroupsService';
