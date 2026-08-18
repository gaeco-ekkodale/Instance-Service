// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using InstanceService.Api.Messaging.Consumers.Classifications.Contracts;
using InstanceService.Models;
using MassTransit.Mediator;
using Messaging.Core.Extensions.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InstanceService.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[Authorize]
public class ClassificationsController : ControllerBase
{
	private readonly IMediator _mediator;

	public ClassificationsController(IMediator mediator)
	{
		_mediator = mediator;
	}

	/// <summary>
	/// An Endpoint to retrieve all classifications.
	/// </summary>
	/// <returns>A list of classifications.</returns>
	//[ProducesResponseType(typeof(ClassificationsListSet), 200)]
	//[HttpGet()]
	//[SwaggerOperation(
	//        Summary = "Retrieve all classifications.",
	//        Description = "An Endpoint to retrieve all classifications.",
	//        OperationId = "GetClassifications",
	//        Tags = new[] { "Classifications", }
	//    )]
	//public async Task<IActionResult> GetClassificationsAsync()
	//{
	//    try
	//    {
	//        var newId = Guid.NewGuid().ToString();
	//        var classifications = await _mediator.SendInternalRequest<GetClassifications, ClassificationsListSet>(new GetClassifications { Id = newId });
	//        return Ok(classifications);
	//    }
	//    catch (Exception ex)
	//    {
	//        // Return a generic error message for production
	//        return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred. Please try again later.");
	//    }
	//}

	/// <summary>
	/// An Endpoint to retrieve all classifications that the user has access to.
	/// </summary>
	/// <returns>A list of classifications filtered by access rights.</returns>
	[ProducesResponseType(typeof(ClassificationsListSet), 200)]
	[HttpGet("usecase/{useCaseId}")]
	[SwaggerOperation(
			Summary = "Retrieve classifications. Filtered by access rights.",
			Description = "An Endpoint to retrieve all classifications that the user has access to.",
			OperationId = "GetClassificationsByUseCaseUserGroup",
			Tags = new[] { "Classifications", }
		)]
	public async Task<IActionResult> GetClassificationsByUseCaseUserGroupAsync(string useCaseId)
	{
		var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);

		var classifications = await _mediator.SendInternalRequest<GetClassificationsFiltered, ClassificationListResponse>(new GetClassificationsFiltered()
		{
			Id = useCaseId,
			Token = token
		});

		return Ok(classifications.Classifications);
	}

	/// <summary>
	/// An Endpoint to retrieve a classification with properties that the user has access to.
	/// </summary>
	/// <param name="useCaseId">The useCase to filter by.</param>
	/// <param name="classificationId">The Id of the classification to be retrieved.</param>
	/// <returns>The classification with the specified Id.</returns>
	[ProducesResponseType(typeof(Classification), 200)]
	[HttpGet("usecase/{useCaseId}/classification/{classificationId}")]
	[SwaggerOperation(
			Summary = "Retrieve classification with properties. Filtered by access rights.",
			Description = "An Endpoint to retrieve a classification with properties that the user has access to.",
			OperationId = "GetClassificationByUseCaseUserGroup",
			Tags = new[] { "Classifications", }
		)]
	public async Task<IActionResult> GetClassificationByUseCaseUserGroupAsync(string useCaseId, string classificationId)
	{
		var token = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);

		var classification = await _mediator.SendInternalRequest<GetClassification, Classification>(new GetClassification()
		{
			ClassificationId = classificationId,
			UseCaseId = useCaseId,
			Token = token
		});

		return Ok(classification);
	}
}
