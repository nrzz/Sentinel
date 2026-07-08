using FluentValidation;

namespace Sentinel.Api.Features.Logs;

public sealed class LogEntryDtoValidator : AbstractValidator<LogEntryDto>
{
    public LogEntryDtoValidator()
    {
        RuleFor(x => x.Service).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Environment).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Level).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(65_536);
    }
}

public sealed class IngestLogsRequestValidator : AbstractValidator<IngestLogsRequest>
{
    public IngestLogsRequestValidator()
    {
        RuleFor(x => x.Logs).NotEmpty().Must(logs => logs.Count <= 1000)
            .WithMessage("A maximum of 1000 log entries can be ingested per request.");
        RuleForEach(x => x.Logs).SetValidator(new LogEntryDtoValidator());
    }
}
