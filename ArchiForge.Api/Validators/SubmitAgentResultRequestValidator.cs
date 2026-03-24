using ArchiForge.Api.Models;

using FluentValidation;

namespace ArchiForge.Api.Validators;

public sealed class SubmitAgentResultRequestValidator : AbstractValidator<SubmitAgentResultRequest>
{
    public SubmitAgentResultRequestValidator()
    {
        RuleFor(x => x.Result)
            .NotNull().WithMessage("Agent result is required.");

        When(_ => true, () =>
        {
            RuleFor(x => x.Result.ResultId)
                .NotEmpty().WithMessage("AgentResult.ResultId is required.");

            RuleFor(x => x.Result.RunId)
                .NotEmpty().WithMessage("AgentResult.RunId is required.");

            RuleFor(x => x.Result.TaskId)
                .NotEmpty().WithMessage("AgentResult.TaskId is required.");

            RuleFor(x => x.Result.Confidence)
                .InclusiveBetween(0.0, 1.0).WithMessage("AgentResult.Confidence must be between 0 and 1.");

            RuleFor(x => x.Result.Claims)
                .NotNull().WithMessage("AgentResult.Claims is required.");

            RuleFor(x => x.Result.EvidenceRefs)
                .NotNull().WithMessage("AgentResult.EvidenceRefs is required.");
        });
    }
}
