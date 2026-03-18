namespace DailyWork.Agents;

/// <summary>
/// Constants for agent keys used in keyed service registration and
/// <c>[FromKeyedServices]</c> injection. Each constant must match the
/// corresponding <see cref="IAgentFactory.AgentName"/> return value.
/// </summary>
public static class AgentKeys
{
    public const string Chat = "chat";

    public const string Goals = "goals";

    public const string Blackjack = "blackjack";

    public const string Knowledge = "knowledge";
}
