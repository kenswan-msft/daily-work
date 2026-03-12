using Microsoft.Agents.AI;

namespace DailyWork.Agents;

public interface IAgentFactory
{
    static abstract string AgentName { get; }

    static abstract string? AgentDescription { get; }

    AIAgent Create();
}
