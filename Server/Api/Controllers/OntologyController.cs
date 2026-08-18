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
using InstanceService.Api.Dto.Ontology;
using InstanceService.Api.Messaging.Consumers.Ontology.Contracts;
using MassTransit.Mediator;
using Messaging.Core.Extensions.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InstanceService.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [Authorize]
    public class OntologyController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IMapper _mapper;

        public OntologyController(IMediator mediator, IMapper mapper)
        {
            _mediator = mediator;
            _mapper = mapper;
        }

        /// <summary>
        /// API call to retrieve all relations for a specific Object.
        /// </summary>
        /// <param name="objectUri">The passed Id whose relations are to be retrieved.</param>
        /// <returns>A list of relations for the given Id.</returns>
        [SwaggerOperation(
            Summary = "Get relations",
            Description = "Returns all relations involving the passed id as subject or object",
            OperationId = "GetRelations",
            Tags = new[] { "Ontology" }
        )]
        [ProducesResponseType(typeof(List<RelationDTO>), 200)]
        [ProducesResponseType(typeof(string), 400)] // Change to string for error messages
        [HttpGet("{objectUri}", Name = "GetRelations")]
        public async Task<IActionResult> GetRelations(string objectUri)
        {
            if (string.IsNullOrWhiteSpace(objectUri))
            {
                return BadRequest("Object URI cannot be null or empty.");
            }

            var result = await _mediator.SendInternalRequest<GetRelationsRequest, GetRelationsResponse>(new GetRelationsRequest
            {
                SourceId = objectUri
            });

            if (result == null || result.Relations == null)
            {
                return BadRequest("No relations found for the specified object URI.");
            }

            return Ok(result.Relations);
        }
    }
}