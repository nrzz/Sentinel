using FluentValidation;

namespace Sentinel.Api.Features.Traces;

public sealed class SpanEntryDtoValidator : AbstractValidator<SpanEntryDto>
{
    public SpanEntryDtoValidator()
    {
        RuleFor(x => x.SpanId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Service).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(32);
    }
}

public sealed class TraceEntryDtoValidator : AbstractValidator<TraceEntryDto>
{
    public TraceEntryDtoValidator()
    {
        RuleFor(x => x.TraceId).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Service).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Status).NotEmpty().MaximumLength(32);
        RuleForEach(x => x.Spans!).SetValidator(new SpanEntryDtoValidator()).When(x => x.Spans is not null);
    }
}

public sealed class IngestTracesRequestValidator : AbstractValidator<IngestTracesRequest>
{
    public IngestTracesRequestValidator()
    {
        RuleFor(x => x.Traces).NotEmpty().Must(traces => traces.Count <= 500)
            .WithMessage("A maximum of 500 traces can be ingested per request.");
        RuleForEach(x => x.Traces).SetValidator(new TraceEntryDtoValidator());
    }
}
