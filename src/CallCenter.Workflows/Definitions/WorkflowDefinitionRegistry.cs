using CallCenter.Core;

namespace CallCenter.Workflows;

/// <summary>
/// Workflow 查找表。
/// </summary>
public sealed class WorkflowDefinitionRegistry(IEnumerable<IWorkflowDefinitionProvider> providers) : IWorkflowDefinitionRegistry
{
    private readonly Dictionary<string, WorkflowDefinition> _definitions = providers
        .SelectMany(provider => provider.GetDefinitions())
        .ToDictionary(definition => definition.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 按名称取一个流程。
    /// </summary>
    public WorkflowDefinition Get(string workflowName)
    {
        if (_definitions.TryGetValue(workflowName, out WorkflowDefinition? definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"Workflow '{workflowName}' is not registered.");
    }

    /// <summary>
    /// 返回所有流程，主要给 catalog 或调试使用。
    /// </summary>
    public IReadOnlyCollection<WorkflowDefinition> GetAll() => _definitions.Values;
}
