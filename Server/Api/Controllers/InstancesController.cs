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
using MassTransit.Mediator;
using Messaging.Core.Extensions.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InstanceService.Api.Controllers;

[Route("{useCaseId}/[controller]")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[Authorize]
public class InstancesController : ControllerBase
{
	private readonly IMediator _mediator;
	private readonly IMapper _mapper;

	public InstancesController(IMediator mediator, IMapper mapper)
	{
		_mediator = mediator;
		_mapper = mapper;
	}

	/// <summary>
	/// Gets the graph of all nodes and relations regarding the use case and user groups.
	/// </summary>
	/// <param name="useCaseId">The id of the use case to filter.</param>
	/// <response code="200">The graph of basic instance information and relations.</response>
	[HttpGet("graph", Name = "GetGraph")]
	[ProducesResponseType(typeof(Dto.Graph), 200)]
	[SwaggerOperation(Tags = ["Instances"])]
	public async Task<IActionResult> GetGraph(string useCaseId)
	{
		GetGraphRequest request = new()
		{
			UseCaseId = useCaseId
		};

		GetGraphResponse response = await _mediator.SendInternalRequest<GetGraphRequest, GetGraphResponse>(request);

		Dto.Graph dto = new()
		{
			Instances = _mapper.Map<IEnumerable<Dto.Instance>>(response.Instances, opts => opts.Items["UseCaseId"] = useCaseId),
			Relations = _mapper.Map<IEnumerable<Dto.InstanceRelation>>(response.Relations)
		};

		return Ok(dto);
	}

	/// <summary>
	/// Gets the graph of all nodes and relations fitting the query.
	/// </summary>
	/// <param name="useCaseId">The id of the use case to filter.</param>
	/// <param name="query">The query used to filter the results.</param>
	/// <response code="200">The graph of basic instance information and relations.</response>
	[HttpGet("filteredGraph", Name = "GetFilteredGraph")]
	[ProducesResponseType(typeof(Dto.Graph), 200)]
	[SwaggerOperation(Tags = ["Instances"])]
	public async Task<IActionResult> GetGraph(string useCaseId, string query)
	{
		var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);

		GetFilteredGraphRequest request = new()
		{
			UseCaseId = useCaseId,
			Token = token,
			TextQuery = query
		};

		GetFilteredGraphResponse response = await _mediator.SendInternalRequest<GetFilteredGraphRequest, GetFilteredGraphResponse>(request);

		Dto.Graph dto = new()
		{
			Instances = _mapper.Map<IEnumerable<Dto.Instance>>(response.Instances, opts => opts.Items["UseCaseId"] = useCaseId),
			Relations = _mapper.Map<IEnumerable<Dto.InstanceRelation>>(response.Relations)
		};

		return Ok(dto);
	}

	/// <summary>
	/// Update a node by id with given data.
	/// </summary>
	/// <param name="useCaseId">The use case ID.</param>
	/// <param name="instanceId">The id of the node to update.</param>
	/// <param name="updateInstanceDto">The data to update the node with.</param>
	/// <response code="204">The node was updated successfully.</response>
	[HttpPut("{instanceId}", Name = "UpdateInstance")]
	[ProducesResponseType(204)]
	[SwaggerOperation(Tags = ["Instances"])]
	public async Task<IActionResult> UpdateInstance(string useCaseId, string instanceId, [FromBody] Dto.Request.UpdateInstance updateInstanceDto)
	{
		// Retrieve the bearer token containing the user groups.
		var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);

		// Retrieve the requried metadata to filter access rights.
		var metadataResponse = await _mediator.SendInternalRequest<GetInstanceMetadataRequest, GetInstanceMetadataResponse>(
			new GetInstanceMetadataRequest { InstanceId = instanceId, UseCaseId = useCaseId, Token = token });

		UpdateInstance request = new()
		{
			InstanceId = instanceId,
			Name = updateInstanceDto.Name,
			Properties = updateInstanceDto.Properties,
			Token = token,
			UseCaseId = useCaseId,
			ClassificationId = metadataResponse.Metadata.ClassificationId
		};

		await _mediator.Send(request);

		return NoContent();
	}

	/// <summary>
	/// Create an instance node with given data.
	/// </summary>
	/// <param name="useCaseId">The use case ID.</param>
	/// <param name="createInstanceDto">The data to create the node with.</param>
	/// <response code="201">The node was created successfully with the id to identify the created instance.</response>
	[HttpPost(Name = "CreateInstance")]
	[ProducesResponseType(typeof(string), 201)]
	[SwaggerOperation(Tags = ["Instances"])]
	public async Task<IActionResult> CreateInstance(string useCaseId, [FromBody] Dto.Request.CreateInstance createInstanceDto)
	{
		// Retrieve the bearer token containing the user groups.
		var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);

		CreateInstanceRequest request = _mapper.Map<CreateInstanceRequest>(createInstanceDto);
		request.useCaseId = useCaseId;
		request.Token = token;

		CreateInstanceResponse response = await _mediator.SendInternalRequest<CreateInstanceRequest, CreateInstanceResponse>(request);

		return Ok(response);
	}

	/// <summary>
	/// Get the metadata of a node by id.
	/// </summary>
	/// <param name="instanceId">The id of the node to get metadata from.</param>
	/// <param name="useCaseId">The id of the use case to filter.</param>
	/// <response code="200">The metadata of the node.</response>
	[HttpGet("{instanceId}", Name = "GetInstance")]
	[ProducesResponseType(typeof(Dto.Metadata), 200)]
	[SwaggerOperation(Tags = ["Instances"])]
	public async Task<IActionResult> GetInstanceMetadata(string instanceId, string useCaseId)
	{
		// Retrieve the bearer token containing the user groups.
		var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);

		GetInstanceMetadataRequest request = new()
		{
			InstanceId = instanceId,
			UseCaseId = useCaseId,
			Token = token
		};

		GetInstanceMetadataResponse response = await _mediator.SendInternalRequest<GetInstanceMetadataRequest, GetInstanceMetadataResponse>(request);

		Dto.Metadata dto = _mapper.Map<Dto.Metadata>(response);

		return Ok(dto);
	}

	/// <summary>
	/// Get the metadata of multiple Nodes by id.
	/// </summary>
	/// <param name="instanceIds">The ids of the nodes to get metadata from, provided in the request body.</param>
	/// <param name="useCaseId">The id of the use case to filter.</param>
	/// <response code="200">The metadata of the nodes inside a enumerable.</response>
	[HttpPost("metadata", Name = "GetInstancesMetadata")]
	[ProducesResponseType(typeof(IEnumerable<Dto.Metadata>), 200)]
	[SwaggerOperation(Tags = ["Instances"])]
	public async Task<IActionResult> GetInstancesMetadata([FromBody] IEnumerable<string> instanceIds, string useCaseId)
	{
		// Retrieve the bearer token containing the user groups.
		var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);

		GetInstancesMetadataRequest request = new()
		{
			InstanceIds = instanceIds,
			UseCaseId = useCaseId,
			Token = token
		};

		GetInstancesMetadataResponse response = await _mediator.SendInternalRequest<GetInstancesMetadataRequest, GetInstancesMetadataResponse>(request);

		List<Dto.Metadata> result = [];
		foreach (var instance in response.InstanceData)
		{
			result.Add(_mapper.Map<Dto.Metadata>(instance));
		}

		return Ok(result);
	}

	/// <summary>
	/// Create a single relation between two nodes.
	/// Subject - has Relation -> Object
	/// </summary>
	/// <param name="useCaseId">The use case ID.</param>
	/// <param name="relationDto">The relation to create, identified by the subject id, the object id and the canonical ontology property URI.</param>
	/// <response code="204">The relation was created successfully.</response>
	[HttpPost("relation", Name = "CreateRelation")]
	[ProducesResponseType(204)]
	[SwaggerOperation(Tags = ["Relations"])]
	public Task<IActionResult> CreateRelation(string useCaseId, [FromBody] Dto.Request.CreateRelation relationDto)
		=> CreateRelationsInternal(useCaseId, [relationDto]);

	/// <summary>
	/// Create multiple relations between nodes based on triples of subject id, object id and predicate URI.
	/// </summary>
	/// <param name="useCaseId">The use case ID.</param>
	/// <param name="relationDtos">The relations to create.</param>
	/// <response code="204">The relations were created successfully.</response>
	[HttpPost("relations", Name = "CreateRelations")]
	[ProducesResponseType(204)]
	[SwaggerOperation(Tags = ["Relations"])]
	public Task<IActionResult> CreateRelations(string useCaseId, [FromBody] IEnumerable<Dto.Request.CreateRelation> relationDtos)
		=> CreateRelationsInternal(useCaseId, relationDtos);

	/// <summary>
	/// Maps the given relation DTOs and dispatches the create request.
	/// </summary>
	/// <param name="useCaseId">The use case ID.</param>
	/// <param name="relationDtos">The relations to create.</param>
	private async Task<IActionResult> CreateRelationsInternal(string useCaseId, IEnumerable<Dto.Request.CreateRelation> relationDtos)
	{
		// Retrieve the bearer token containing the user groups.
		var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);

		IEnumerable<InstanceRelation> relations = _mapper.Map<IEnumerable<InstanceRelation>>(relationDtos);

		await _mediator.Send(new CreateRelations()
		{
			Relations = relations,
			Token = token,
			UseCaseId = useCaseId
		});

		return NoContent();
	}

	/// <summary>
	/// Deletes an instance with the specified Id.
	/// </summary>
	/// <param name="useCaseId">The use case ID.</param>
	/// <param name="instanceId">The id of the instance to be deleted.</param>
	/// <response code="204">The instance was deleted successfully.</response>
	[HttpDelete("{instanceId}", Name = "DeleteInstance")]
	[ProducesResponseType(204)]
	[SwaggerOperation(Tags = ["Instances"])]
	public async Task<IActionResult> DeleteInstance(string useCaseId, string instanceId)
	{
		// Retrieve the bearer token containing the user groups.
		var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);

		// Retrieve the requried metadata to filter access rights.
		var metadataResponse = await _mediator.SendInternalRequest<GetInstanceMetadataRequest, GetInstanceMetadataResponse>(
			new GetInstanceMetadataRequest { InstanceId = instanceId, UseCaseId = useCaseId, Token = token });

		await _mediator.Send(new DeleteInstance()
		{
			Id = instanceId,
			Token = token,
			UseCaseId = useCaseId,
			classificationId = metadataResponse.Metadata.ClassificationId
		});
		return NoContent();
	}

	/// <summary>
	/// Deletes all relations of the instance with the specified id.
	/// </summary>
	/// <param name="useCaseId">The use case ID.</param>
	/// <param name="instanceId">The id of the instance, whose relations are to be deleted.</param>
	/// <response code="204">The relations were deleted successfully.</response>
	[HttpDelete("{instanceId}/relations", Name = "DeleteRelations")]
	[ProducesResponseType(204)]
	[SwaggerOperation(Tags = ["Relations"])]
	public async Task<IActionResult> DeleteRelations(string useCaseId, string instanceId)
	{
		// Retrieve the bearer token containing the user groups.
		var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);

		// Retrieve the requried metadata to filter access rights.
		var metadataResponse = await _mediator.SendInternalRequest<GetInstanceMetadataRequest, GetInstanceMetadataResponse>(
			new GetInstanceMetadataRequest { InstanceId = instanceId, UseCaseId = useCaseId, Token = token });

		await _mediator.Send(new DeleteRelations()
		{
			InstanceId = instanceId,
			Token = token,
			useCaseId = useCaseId,
			classificationId = metadataResponse.Metadata.ClassificationId
		});
		return NoContent();
	}

	/// <summary>
	/// Deletes a relation of the two instances with the specified label.
	/// Subject - has Relation -> Object
	/// </summary>
	/// <param name="useCaseId">The id of the usecase, in which the relation is to be deleted.</param>
	/// <param name="instanceId">The id of the subject instance that uses the relation </param>
	/// <param name="objectId">The id of the object instance that uses the relation </param>
	/// <param name="predicateUri">The canonical ontology property URI identifying the relation</param>
	/// <response code="204">The relation was deleted successfully.</response>
	[HttpDelete("{instanceId}/relation", Name = "DeleteRelation")]
	[ProducesResponseType(204)]
	[SwaggerOperation(Tags = ["Relations"])]
	public async Task<IActionResult> DeleteRelation(string useCaseId, string instanceId, string objectId, string predicateUri)
	{
		// Retrieve the bearer token containing the user groups.
		var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);

		// Retrieve the requried metadata to filter access rights.
		var metadataResponse = await _mediator.SendInternalRequest<GetInstanceMetadataRequest, GetInstanceMetadataResponse>(
			new GetInstanceMetadataRequest { InstanceId = instanceId, UseCaseId = useCaseId, Token = token });

		await _mediator.Send(new DeleteRelation()
		{
			InstanceId = instanceId,
			Token = token,
			useCaseId = useCaseId,
			classificationId = metadataResponse.Metadata.ClassificationId,
			ObjectId = objectId,
			PredicateUri = predicateUri
		});
		return NoContent();
	}
}
