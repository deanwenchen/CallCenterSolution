using CallCenter.Core;

namespace CallCenter.Workflows;

/// <summary>
/// 写流程定义时用的小工具，避免每个文件重复写 timeout/retry 等默认值。
/// </summary>
internal static class WorkflowDefinitionDsl
{
    public static WorkflowStepDefinition Step(
        string name,
        string businessActionName,
        string? compensationBusinessActionName = null,
        bool humanInTheLoop = false)
    {
        return new WorkflowStepDefinition(
            name,
            businessActionName,
            Timeout: TimeSpan.FromSeconds(30),
            MaxRetries: 3,
            compensationBusinessActionName,
            humanInTheLoop);
    }

    public static WorkflowEdgeDefinition Edge(string from, string to)
    {
        return new WorkflowEdgeDefinition(from, to, "previous.CanContinue");
    }
}
