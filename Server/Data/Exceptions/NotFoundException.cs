// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

namespace InstanceService.Data.Exceptions;

/// <summary>
/// The exception that is thrown when a specific entity is not found.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class for a specific entity type and identifier.
    /// </summary>
    /// <param name="type">The type of the entity that was not found.</param>
    /// <param name="id">The identifier of the entity that was not found.</param>
    public NotFoundException(Type type, string id) : base($"Entity of type {type.Name} with id {id} not found")
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerEx">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public NotFoundException(string message, Exception innerEx) : base(message, innerEx)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public NotFoundException(string message) : base(message)
    {
    }
}
