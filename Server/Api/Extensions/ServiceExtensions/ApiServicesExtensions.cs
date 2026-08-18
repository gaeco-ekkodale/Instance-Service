// Copyright (c) 2025 Ekkodale GmbH. All rights reserved.
//
// This file is part of the gaeco platform system.
//
// Use of this file is governed by the terms of the license
// in LICENSE.md at the root of this repository.
// Unauthorized copying, modification, distribution, or use of this file,
// via any medium, is strictly prohibited except as expressly permitted
// under that license.

using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace InstanceService.Api.Extensions.ServiceExtensions;

/// <summary>
/// Provides extension methods for adding API and middleware services to the application.
/// This category includes the configuration of web API-specific services, such as setting up MVC, CORS policies, API versioning, Swagger, and more.
/// </summary>
public static class ApiServicesExtensions
{
    private static IConfiguration _configuration;

    /// <summary>
    /// Adds API and middleware services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        return services;
    }

    /// <summary>
    /// Extension method to add Swagger generation services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="config">The <see cref="IConfiguration"/> containing Swagger and Keycloak configuration settings.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddSwagger(this IServiceCollection services, IConfiguration config)
    {
        _configuration = config;
        var authority = $"{_configuration["Keycloak:ServerUrl"]}/realms/{_configuration["Keycloak:Realm"]}";

        services.AddSwaggerGen(c =>
        {
            c.SchemaFilter<XEnumNamesSchemaFilter>();
            var xmlFilename = $"{Assembly.GetEntryAssembly()!.GetName().Name}.xml";
            if (File.Exists(Path.Combine(AppContext.BaseDirectory, xmlFilename)))
            {
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename), true);
            }
            c.UseOneOfForPolymorphism();
            c.EnableAnnotations();

            c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    Implicit = new OpenApiOAuthFlow // Assuming the response type is 'code'
                    {
                        AuthorizationUrl = new Uri($"{authority}/protocol/openid-connect/auth"),
                        TokenUrl = new Uri($"{authority}/protocol/openid-connect/token"),
                        Scopes = new Dictionary<string, string> { { "openid", "" }, { "profile", "" }, { "email", "" } }
                    },
                    // Machine-to-machine callers (e.g. an MCP gateway consuming this spec) authenticate
                    // with a Keycloak service account instead of a browser login. Declaring the flow lets
                    // spec-driven clients discover the token endpoint on their own.
                    ClientCredentials = new OpenApiOAuthFlow
                    {
                        TokenUrl = new Uri($"{authority}/protocol/openid-connect/token"),
                        Scopes = new Dictionary<string, string> { { "openid", "" }, { "profile", "" }, { "email", "" } }
                    }
                }
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "oauth2"
                        }
                    },
                    new string[] { }
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Extension method to add Swagger and Swagger UI services to the specified <see cref="WebApplication"/>
    /// in development environment.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to add Swagger and Swagger UI services to.</param>
    public static void AddSwagger(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                var clientId = _configuration["Keycloak:Client"];
                c.SwaggerEndpoint("/swagger/v1/swagger.json", clientId);
                c.OAuthClientId(clientId);
                c.OAuthAppName(clientId);
            });
        }
    }

    /// <summary>
    /// Schema filter for the enumeration names
    /// </summary>
    public class XEnumNamesSchemaFilter : ISchemaFilter
    {
        /// <summary>
        /// Applies enumeration names as strings in schema
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="context"></param>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            var type = context.Type;
            if (type.IsEnum)
            {
                // Add enum type information once
                // x-enum-names for Nswag
                if (schema.Extensions.ContainsKey("x-enum-varnames")) return;

                var valuesArr = new OpenApiArray();
                valuesArr.AddRange(Enum.GetNames(context.Type)
                                                .Select(value => new OpenApiString(value)));

                schema.Extensions.Add(
                    "x-enum-varnames",
                    valuesArr
                );
            }
        }
    }
}