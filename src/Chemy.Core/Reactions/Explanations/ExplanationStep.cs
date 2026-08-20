using System.Text;

namespace Chemy.Core.Reactions.Explanations;

public record ExplanationStep(int StepNumber, string Title, string Description, string Detail);

public record BalancedReactionWithSteps(
    Reaction BalancedReaction,
    IReadOnlyList<ExplanationStep> Steps
)
{
    public string FormattedExplanation
    {
        get
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Step-by-Step Reaction Balancing: {BalancedReaction}");
            sb.AppendLine();
            foreach (var step in Steps)
            {
                sb.AppendLine($"### Step {step.StepNumber}: {step.Title}");
                sb.AppendLine(step.Description);
                if (!string.IsNullOrWhiteSpace(step.Detail))
                {
                    sb.AppendLine("```text");
                    sb.AppendLine(step.Detail);
                    sb.AppendLine("```");
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
