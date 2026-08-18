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

using InstanceService.Api.Dto;
using InstanceService.Api.Messaging.Consumers.Internal.Contracts;

namespace InstanceService.Api.MapperProfiles;

public class InstanceMetadataProfile : Profile
{
    public InstanceMetadataProfile()
    {
        // Mapping from Dto.Metadata to Models.InstanceMetaData
        CreateMap<Dto.Metadata, Models.InstanceMetaData>()
            .ForMember(dest => dest.Properties, opt => opt.MapFrom(src => src.Properties.ToDictionary(p => p.Name, p => p.Value)));

        CreateMap<InstanceData, Metadata>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Metadata.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Metadata.Name))
            .ForMember(dest => dest.ClassificationId, opt => opt.MapFrom(src => src.Metadata.ClassificationId))
            .ForMember(dest => dest.Properties, opt => opt.MapFrom(src => src.MetadataProperties));

        CreateMap<GetInstanceMetadataResponse, Metadata>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Metadata.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Metadata.Name))
            .ForMember(dest => dest.ClassificationId, opt => opt.MapFrom(src => src.Metadata.ClassificationId))
            .ForMember(dest => dest.Properties, opt => opt.MapFrom(src => src.MetadataProperties));
    }
}
