using FluentValidation;
using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Authentication;

internal static class RegisterEndpoint
{
    public static async Task<IResult> Handle(
        RegisterRequest request,
        IValidator<RegisterRequest> validator,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationProblem(validation.ToDictionary());
        }

        var result = await authService.RegisterAsync(
            request.Email,
            request.Password,
            request.DisplayName,
            request.TenantName,
            request.TenantSlug,
            cancellationToken);

        if (result is null)
        {
            return ApiResults.Conflict("A user or tenant with the provided identifiers already exists.");
        }

        return Results.Created(
            $"/api/v1/users/{result.User.Id}",
            MapResponse(result));
    }

    private static AuthResponse MapResponse(AuthResult result)
    {
        var expiresIn = (int)Math.Max(
            0,
            (result.Tokens.AccessTokenExpiresAt - DateTimeOffset.UtcNow).TotalSeconds);

        return new AuthResponse(
            result.Tokens.AccessToken,
            result.Tokens.RefreshToken,
            result.Tokens.AccessTokenExpiresAt,
            expiresIn,
            new AuthUserResponse(
                result.User.Id,
                result.User.Email,
                result.User.DisplayName,
                result.User.TenantId,
                result.User.Roles,
                result.User.Permissions));
    }
}
