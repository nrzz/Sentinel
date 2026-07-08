using FluentValidation;

namespace Sentinel.Api.Features.Search;

public sealed class CreateSavedSearchRequestValidator : AbstractValidator<CreateSavedSearchRequest>
{
    public CreateSavedSearchRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Query).NotEmpty().MaximumLength(2048);
    }
}

public sealed class UpdateSavedSearchRequestValidator : AbstractValidator<UpdateSavedSearchRequest>
{
    public UpdateSavedSearchRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Query).NotEmpty().MaximumLength(2048);
    }
}
