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
using Confluent.Kafka.Admin;
using Guideline.Model.Model;
using InstanceService.Api.Dto.Request;
using InstanceService.Api.Messaging.Consumers.Internal.Contracts;
using InstanceService.Models;
using InstanceService.Models.Enum;
using static InstanceService.Api.Dto.Request.CreateInstance;

namespace InstanceService.Api.MapperProfiles;

public class InstanceProfile : Profile
{
    public InstanceProfile()
    {
        CreateMap<CreateInstance, CreateInstanceRequest>()
            .ForMember(dest => dest.useCaseId, opt => opt.Ignore())
            .ForMember(dest => dest.Token, opt => opt.Ignore());
        CreateMap<CreateInstanceWithRelation, CreateInstanceRequest.CreateInstanceWithRelation>();

        CreateMap<Instance, Dto.Instance>()
            .ForMember(dest => dest.Accessibility, opt => opt.Ignore())
            .ForMember(dest => dest.ClassificationName, opt => opt.MapFrom<ClassificationNameResolver>())
            .ForMember(dest => dest.GuidelineName, opt => opt.MapFrom<GuidelineNameResolver>());

        CreateMap<(Instance instance, Accessibility accessibility), Dto.Instance>()
            .ForMember(dest => dest.Accessibility, opt => opt.MapFrom(src => src.accessibility))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.instance.Name))
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.instance.Id))
            .ForMember(dest => dest.ClassificationId, opt => opt.MapFrom(src => src.instance.ClassificationId))
            .ForMember(dest => dest.ClassificationName, opt => opt.MapFrom<ClassificationNameResolver>())
            .ForMember(dest => dest.GuidelineName, opt => opt.MapFrom<GuidelineNameResolver>());

        CreateMap<Dto.Instance, Instance>();
    }
}