// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using AutoMapper;
using InstanceService.Api.Messaging.Consumers.Internal.Contracts;
using InstanceService.Models;
using InstanceService.Models.Enum;
using static InstanceService.Api.Dto.Request.CreateInstance;

namespace InstanceService.Api.MapperProfiles;

public class InstanceRelationProfile : Profile
{
    public InstanceRelationProfile()
    {
        CreateMap<Dto.Request.CreateRelation, InstanceRelation>();
        CreateMap<InstanceRelation, Dto.Request.CreateRelation>();

        // Dto.InstanceRelation is a read model (it carries the resolved label), so it is only mapped outbound.
        CreateMap<InstanceRelation, Dto.InstanceRelation>();

        CreateMap<CreateInstanceRequest.CreateInstanceWithRelation, InstanceRelation>()
        .ForMember(dest => dest.SubjectId, opt => opt.MapFrom((src, dest, destMember, context) =>
        {
            string? id = context.Items["Id"] as string;
            return src.Direction == Direction.From ? src.InstanceId : id;
        }))
        .ForMember(dest => dest.ObjectId, opt => opt.MapFrom((src, dest, destMember, context) =>
        {
            string? id = context.Items["Id"] as string;
            return src.Direction == Direction.From ? id : src.InstanceId;
        }));
    }
}
