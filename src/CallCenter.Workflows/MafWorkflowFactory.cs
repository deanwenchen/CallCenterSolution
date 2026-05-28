using CallCenter.Application;
using CallCenter.Domain;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows;

public sealed class MafWorkflowFactory(IBusinessActionRegistry businessActionRegistry)
{
    public Workflow Create(WorkflowDefinition definition, string? startStepName = null)
    {
        if (definition.Steps.Count == 0)
        {
            throw new InvalidOperationException($"Workflow '{definition.Name}' must contain at least one step.");
        }

        var executors = new Dictionary<string, Executor>(StringComparer.OrdinalIgnoreCase);
        WorkflowStepDefinition firstStep = ResolveStartStep(definition, startStepName);
        HashSet<string> reachableStepNames = GetReachableStepNames(definition, firstStep.Name);
        executors[firstStep.Name] = new StartBusinessActionExecutor(firstStep, businessActionRegistry);

        foreach (WorkflowStepDefinition step in definition.Steps.Where(step =>
                     reachableStepNames.Contains(step.Name) &&
                     !string.Equals(step.Name, firstStep.Name, StringComparison.OrdinalIgnoreCase)))
        {
            executors[step.Name] = new BusinessActionStepExecutor(step, businessActionRegistry);
        }

        // MAF Workflow 是业务动作的唯一运行入口；恢复时从 CurrentStep 构建续跑子图，避免重跑已完成步骤。
        WorkflowBuilder builder = new(executors[firstStep.Name]);
        builder.WithName(definition.Name)
            .WithDescription($"{definition.Capability} capability workflow");

        foreach (WorkflowEdgeDefinition edge in definition.Edges)
        {
            if (!executors.ContainsKey(edge.FromStep) || !executors.ContainsKey(edge.ToStep))
            {
                continue;
            }

            builder.AddEdge<BusinessActionResult>(
                executors[edge.FromStep],
                executors[edge.ToStep],
                result => result?.CanContinue == true,
                edge.Condition);
        }

        WorkflowStepDefinition outputStep = definition.Steps.Last(step => reachableStepNames.Contains(step.Name));
        builder.WithOutputFrom(executors[outputStep.Name]);
        return builder.Build();
    }

    private static WorkflowStepDefinition ResolveStartStep(WorkflowDefinition definition, string? startStepName)
    {
        if (string.IsNullOrWhiteSpace(startStepName))
        {
            return definition.Steps[0];
        }

        return definition.Steps.FirstOrDefault(step => string.Equals(step.Name, startStepName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Workflow '{definition.Name}' does not contain step '{startStepName}'.");
    }

    private static HashSet<string> GetReachableStepNames(WorkflowDefinition definition, string startStepName)
    {
        var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<string>();
        queue.Enqueue(startStepName);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            if (!reachable.Add(current))
            {
                continue;
            }

            foreach (WorkflowEdgeDefinition edge in definition.Edges.Where(edge => string.Equals(edge.FromStep, current, StringComparison.OrdinalIgnoreCase)))
            {
                queue.Enqueue(edge.ToStep);
            }
        }

        return reachable;
    }
}
