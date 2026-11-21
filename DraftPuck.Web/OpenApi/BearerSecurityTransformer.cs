using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace DraftPuck.Web.OpenApi;

/// <summary>
/// Describes the API's JWT auth in the OpenAPI document, so Swagger UI shows an Authorize button and a lock on the
/// endpoints that need sign-in. To get a token, call POST /api/Users (a new guest) or POST /api/auth/login and copy
/// its <c>jwtToken</c>.
/// </summary>
public class BearerSecurityTransformer : IOpenApiDocumentTransformer, IOpenApiOperationTransformer
{
    public const string SchemeName = "Bearer";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken ct)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[SchemeName] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "The jwtToken from POST /api/Users (a new guest) or POST /api/auth/login.",
        };
        return Task.CompletedTask;
    }

    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken ct)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (!metadata.OfType<IAuthorizeData>().Any() || metadata.OfType<IAllowAnonymous>().Any())
            return Task.CompletedTask;

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(SchemeName, context.Document)] = [] });
        return Task.CompletedTask;
    }
}
