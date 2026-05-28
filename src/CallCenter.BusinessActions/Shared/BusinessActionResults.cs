using CallCenter.Domain;

namespace CallCenter.BusinessActions;

/// <summary>
/// 创建业务动作结果的共享帮助方法。
/// </summary>
internal static class BusinessActionResults
{
    /// <summary>
    /// 创建成功状态的业务动作结果。
    /// </summary>
    /// <param name="context">业务动作上下文。</param>
    /// <param name="message">返回消息。</param>
    /// <param name="data">业务数据。</param>
    /// <returns>成功结果。</returns>
    public static BusinessActionResult Success(BusinessActionContext context, string message, Dictionary<string, string> data)
    {
        return new BusinessActionResult(
            context.StepName,
            BusinessActionExecutionStatus.Succeeded,
            message,
            data,
            context.Session,
            context.WorkflowName);
    }

    /// <summary>
    /// 复制业务数据字典，避免多个 Step 共享可变引用。
    /// </summary>
    /// <param name="data">原始业务数据。</param>
    /// <returns>复制后的业务数据。</returns>
    public static Dictionary<string, string> Merge(Dictionary<string, string> data)
    {
        return new Dictionary<string, string>(data, StringComparer.OrdinalIgnoreCase);
    }
}
