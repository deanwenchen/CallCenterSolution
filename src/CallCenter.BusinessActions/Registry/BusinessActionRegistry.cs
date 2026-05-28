using CallCenter.Application;

namespace CallCenter.BusinessActions;

/// <summary>
/// 基于依赖注入集合构建的业务动作注册表。
/// </summary>
public sealed class BusinessActionRegistry(IEnumerable<IBusinessAction> businessActions) : IBusinessActionRegistry
{
    private readonly Dictionary<string, IBusinessAction> _businessActions = businessActions.ToDictionary(businessAction => businessAction.Name, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 按业务动作名称解析实现。
    /// </summary>
    /// <param name="businessActionName">业务动作名称。</param>
    /// <returns>业务动作实现。</returns>
    public IBusinessAction Resolve(string businessActionName)
    {
        if (_businessActions.TryGetValue(businessActionName, out IBusinessAction? businessAction))
        {
            return businessAction;
        }

        throw new KeyNotFoundException($"BusinessAction '{businessActionName}' is not registered.");
    }
}
