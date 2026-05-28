using CallCenter.Application;
using CallCenter.Domain;
using Microsoft.Agents.AI.Workflows;

namespace CallCenter.Workflows;

/// <summary>
/// 将平台无关的 WorkflowDefinition 转换为 MAF Workflow 实例。
/// </summary>
public sealed class MafWorkflowFactory(IBusinessActionRegistry businessActionRegistry, IEnumerable<IWorkflowPermissionProvider> permissionProviders)
{
    /// <summary>
    /// 根据 Workflow 定义创建可执行的 MAF Workflow。
    /// </summary>
    /// <param name="definition">Workflow 静态定义。</param>
    /// <param name="startStepName">可选起始 Step，用于恢复流程时构建子图。</param>
    /// <returns>MAF Workflow 实例。</returns>
    public Workflow Create(WorkflowDefinition definition, string? startStepName = null)
    {
        if (definition.Steps.Count == 0)
        {
            throw new InvalidOperationException($"Workflow '{definition.Name}' must contain at least one step.");
        }

        var executors = new Dictionary<string, Executor>(StringComparer.OrdinalIgnoreCase);
        WorkflowStepDefinition firstStep = ResolveStartStep(definition, startStepName);
        HashSet<string> reachableStepNames = GetReachableStepNames(definition, firstStep.Name);

        // 首个 Executor 的输入是 WorkflowExecutionRequest，负责把入口请求转换为首个业务动作上下文。
        executors[firstStep.Name] = new StartBusinessActionExecutor(firstStep, businessActionRegistry, permissionProviders);

        foreach (WorkflowStepDefinition step in definition.Steps.Where(step =>
                     reachableStepNames.Contains(step.Name) &&
                     !string.Equals(step.Name, firstStep.Name, StringComparison.OrdinalIgnoreCase)))
        {
            // 后续 Executor 的输入统一是上一个 BusinessActionResult，形成 MAF Workflow 内部的数据流。
            executors[step.Name] = new BusinessActionStepExecutor(step, businessActionRegistry, permissionProviders);
        }

        // MAF Workflow 是业务动作的唯一运行入口；恢复时从 CurrentStep 构建续跑子图，避免重跑已完成步骤。
        WorkflowBuilder builder = new(executors[firstStep.Name]);
        builder.WithName(definition.Name)
            .WithDescription($"{definition.Capability} capability workflow");

        foreach (WorkflowEdgeDefinition edge in definition.Edges)
        {
            // Resume 时只构建从 CurrentStep 可达的子图。
            // 不可达边直接跳过，避免续跑时把已经完成的前置 Step 加回执行图。
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
        // Workflow 输出取可达子图的最后一个 Step，最终由 MafWorkflowRuntime 转换为 ConversationResponse。
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
        // 用静态边关系计算续跑子图，保证 Resume 不依赖外部系统状态重新推断流程位置。
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
