using CallCenter.Application;

namespace CallCenter.BusinessActions;

public sealed class BusinessActionRegistry(IEnumerable<IBusinessAction> businessActions) : IBusinessActionRegistry
{
    private readonly Dictionary<string, IBusinessAction> _businessActions = businessActions.ToDictionary(businessAction => businessAction.Name, StringComparer.OrdinalIgnoreCase);

    public IBusinessAction Resolve(string businessActionName)
    {
        if (_businessActions.TryGetValue(businessActionName, out IBusinessAction? businessAction))
        {
            return businessAction;
        }

        throw new KeyNotFoundException($"BusinessAction '{businessActionName}' is not registered.");
    }
}
