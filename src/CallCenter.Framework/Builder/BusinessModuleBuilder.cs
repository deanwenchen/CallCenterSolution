// TODO: Production implementation — see PRD Section 7.4 (DevX Builder)
namespace CallCenter.Framework.Builder;

/// <summary>
/// Fluent builder for composing business modules (skills + workflows + defaults).
/// Part of the extensibility layer that allows non-engineers to configure AI agents.
/// TODO: Implement WithSkill<T>(), WithWorkflow<T>(), WithDefaults()
/// </summary>
public class BusinessModuleBuilder
{
    // TODO: Implement WithSkill<T>(), WithWorkflow<T>(), WithDefaults()
    public BusinessModuleBuilder WithSkill<T>() where T : class => this;
    public BusinessModuleBuilder WithWorkflow<T>() where T : class => this;
    public BusinessModuleBuilder WithDefaults() => this;
}
