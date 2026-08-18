// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using System.Web;
using InstanceService.Api.Dto.Ontology;
using InstanceService.Api.Messaging.Consumers.Ontology.Contracts;
using InstanceService.Api.Utilities.Provider;
using Messaging.Core.Abstractions;

namespace InstanceService.Api.Messaging.Consumers.Ontology
{
    /// <summary>
    /// Represents a consumer for getting relations.
    /// </summary>
    public class GetRelationsConsumer : IInternalRequestConsumer<GetRelationsRequest, GetRelationsResponse>
    {
        public ILogger<IInternalRequestConsumer<GetRelationsRequest, GetRelationsResponse>> Logger { get; set; }

        private readonly IOntologyProvider _graphProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetRelationsConsumer"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="graphProvider">The ontology graph provider.</param>
        public GetRelationsConsumer(
            ILogger<IInternalRequestConsumer<GetRelationsRequest, GetRelationsResponse>> logger,
            IOntologyProvider graphProvider)
        {
            Logger = logger;
            _graphProvider = graphProvider;
        }

        public GetRelationsConsumer(IOntologyProvider graphProvider)
        {
            _graphProvider = graphProvider;
        }

        /// <summary>
        /// Consumes the internal request to get relations.
        /// </summary>
        /// <param name="request">The request to get relations.</param>
        /// <returns>The response containing the relations.</returns>
        public async Task<GetRelationsResponse> ConsumeInternal(GetRelationsRequest request)
        {
            string decodedObjectUri = HttpUtility.UrlDecode(request.SourceId);

            if (string.IsNullOrWhiteSpace(decodedObjectUri))
            {
                throw new InvalidDataException("The provided Uri is invalid");
            }

            var relations = await _graphProvider.GetRelationsForClassAsync(decodedObjectUri);

            List<RelationDTO> relationDtos = relations.Select(r => new RelationDTO
            {
                SubjectId = r.DomainUri,
                PredicateId = r.PropertyUri,
                Label = r.Label,
                ObjectId = r.RangeUri
            }).ToList();

            return new GetRelationsResponse { Relations = relationDtos };
        }
    }
}
