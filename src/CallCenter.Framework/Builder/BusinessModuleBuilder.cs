// TODO: Production implementation — see PRD Section 7.4 (DevX Builder)
namespace CallCenter.Framework.Builder;

public class BusinessModuleBuilder
{
    // TODO: Implement WithSkill<T>(), WithWorkflow<T>(), WithDefaults()
    public BusinessModuleBuilder WithSkill<T>() where T : class => this;
    public BusinessModuleBuilder WithWorkflow<T>() where T : class => this;
    public BusinessModuleBuilder WithDefaults() => this;
}
