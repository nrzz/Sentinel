using FluentValidation;

namespace Sentinel.Api.Features.Metrics;

public sealed class MetricEntryDtoValidator : AbstractValidator<MetricEntryDto>
{
    public MetricEntryDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Service).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Environment).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Unit).MaximumLength(64);
    }
}

public sealed class IngestMetricsRequestValidator : AbstractValidator<IngestMetricsRequest>
{
    public IngestMetricsRequestValidator()
    {
        RuleFor(x => x.Metrics).NotEmpty().Must(metrics => metrics.Count <= 1000)
            .WithMessage("A maximum of 1000 metrics can be ingested per request.");
        RuleForEach(x => x.Metrics).SetValidator(new MetricEntryDtoValidator());
    }
}
