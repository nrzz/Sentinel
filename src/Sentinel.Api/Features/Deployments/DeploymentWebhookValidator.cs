using FluentValidation;

namespace Sentinel.Api.Features.Deployments;

public sealed class DeploymentWebhookRequestValidator : AbstractValidator<DeploymentWebhookRequest>
{
    public DeploymentWebhookRequestValidator()
    {
        RuleFor(x => x.Service).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Version).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Environment).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(64);
        RuleFor(x => x.CommitSha).MaximumLength(128);
        RuleFor(x => x.Repository).MaximumLength(512);
    }
}
