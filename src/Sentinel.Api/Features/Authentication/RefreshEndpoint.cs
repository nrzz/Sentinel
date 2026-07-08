using FluentValidation;
using Sentinel.Infrastructure.Identity;

namespace Sentinel.Api.Features.Authentication;

internal static class RefreshEndpoint
{
    public static async Task<IResult> Handle(
        RefreshRequest request,
        IValidator<RefreshRequest> validator,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return ApiResults.ValidationProblem(validation.ToDictionary());
        }

        var result = await authService.RefreshAsync(request.RefreshToken, cancellationToken);
        if (result is null)
        {
            return ApiResults.Unauthorized("Invalid or expired refresh token.");
        }

        var expiresIn = (int)Math.Max(
            0,
            (result.Tokens.AccessTokenExpiresAt - DateTimeOffset.UtcNow).TotalSeconds);

        return Results.Ok(new AuthResponse(
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
                result.User.Permissions)));
    }
}
