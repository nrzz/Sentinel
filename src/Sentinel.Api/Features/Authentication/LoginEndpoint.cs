using FluentValidation;
using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Authentication;

internal static class LoginEndpoint
{
    public static async Task<IResult> Handle(
        LoginRequest request,
        IValidator<LoginRequest> validator,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationProblem(validation.ToDictionary());
        }

        var result = await authService.LoginAsync(
            request.Email,
            request.Password,
            request.TenantId,
            cancellationToken);

        if (result is null)
        {
            return ApiResults.Unauthorized("Invalid email, password, or tenant access.");
        }

        return Results.Ok(MapResponse(result));
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
