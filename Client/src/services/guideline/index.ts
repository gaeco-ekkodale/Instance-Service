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

export type { Assembly } from './models/Assembly';
export type { BadRequestResult } from './models/BadRequestResult';
export type { BatchSearch } from './models/BatchSearch';
export { CallingConventions } from './models/CallingConventions';
export type { ClassificationDTO } from './models/ClassificationDTO';
export type { ClassificationPropertyDTO } from './models/ClassificationPropertyDTO';
export type { ComplexData } from './models/ComplexData';
export type { ConstructorInfo } from './models/ConstructorInfo';
export type { CreatedResult } from './models/CreatedResult';
export type { CustomAttributeData } from './models/CustomAttributeData';
export type { CustomAttributeNamedArgument } from './models/CustomAttributeNamedArgument';
export type { CustomAttributeTypedArgument } from './models/CustomAttributeTypedArgument';
export { EventAttributes } from './models/EventAttributes';
export type { EventInfo } from './models/EventInfo';
export { FieldAttributes } from './models/FieldAttributes';
export type { FieldInfo } from './models/FieldInfo';
export { GenericParameterAttributes } from './models/GenericParameterAttributes';
export type { Guideline } from './models/Guideline';
export type { IClassification } from './models/IClassification';
export type { IClassificationMapping } from './models/IClassificationMapping';
export type { IClassificationProperty } from './models/IClassificationProperty';
export type { IClassificationRelation } from './models/IClassificationRelation';
export type { IComplexDataItem } from './models/IComplexDataItem';
export type { IComplexDataTreeNode } from './models/IComplexDataTreeNode';
export type { ICustomAttributeProvider } from './models/ICustomAttributeProvider';
export type { IDomain } from './models/IDomain';
export type { IGuideline } from './models/IGuideline';
export type { IMapping } from './models/IMapping';
export type { IntPtr } from './models/IntPtr';
export type { IOutputFormatter } from './models/IOutputFormatter';
export type { IParameterMapping } from './models/IParameterMapping';
export type { IProperty } from './models/IProperty';
export type { IPropertyAssignment } from './models/IPropertyAssignment';
export type { IPropertySet } from './models/IPropertySet';
export { LayoutKind } from './models/LayoutKind';
export type { MemberInfo } from './models/MemberInfo';
export { MemberTypes } from './models/MemberTypes';
export { MethodAttributes } from './models/MethodAttributes';
export type { MethodBase } from './models/MethodBase';
export { MethodImplAttributes } from './models/MethodImplAttributes';
export type { MethodInfo } from './models/MethodInfo';
export type { Module } from './models/Module';
export type { ModuleHandle } from './models/ModuleHandle';
export type { NoContentResult } from './models/NoContentResult';
export type { NotFoundResult } from './models/NotFoundResult';
export { ParameterAttributes } from './models/ParameterAttributes';
export type { ParameterInfo } from './models/ParameterInfo';
export { ParameterLocation } from './models/ParameterLocation';
export { ParameterMappingDirection } from './models/ParameterMappingDirection';
export { PropertyAttributes } from './models/PropertyAttributes';
export type { PropertyInfo } from './models/PropertyInfo';
export type { PropertySetDTO } from './models/PropertySetDTO';
export type { RuntimeFieldHandle } from './models/RuntimeFieldHandle';
export type { RuntimeMethodHandle } from './models/RuntimeMethodHandle';
export type { RuntimeTypeHandle } from './models/RuntimeTypeHandle';
export { SecurityRuleSet } from './models/SecurityRuleSet';
export type { SimpleClassificationDTO } from './models/SimpleClassificationDTO';
export { SourceSystems } from './models/SourceSystems';
export { Status } from './models/Status';
export { StorageType } from './models/StorageType';
export type { StructLayoutAttribute } from './models/StructLayoutAttribute';
export type { Type } from './models/Type';
export { TypeAttributes } from './models/TypeAttributes';
export type { TypeInfo } from './models/TypeInfo';
export { UsageType } from './models/UsageType';

export { ClassificationService } from './services/ClassificationService';
export { GuidelineService } from './services/GuidelineService';
