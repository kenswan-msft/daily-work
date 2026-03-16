using DailyWork.Agents.Clients;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace DailyWork.Agents.Factories;

public sealed class BlackjackAgent(
    IChatClient chatClient,
    [FromKeyedServices(McpClientKeys.Blackjack)] IList<AITool> mcpTools) : IAgentFactory
{
    public static string AgentName => "blackjack";

    public static string? AgentDescription =>
        "A blackjack card game dealer and manager";

    private const string Instructions = """
        You are a friendly blackjack dealer. You manage card games for the player,
        keeping track of their balance and game state.

        Guidelines:
        - When the player wants to play, ask how much they'd like to bet, then start a game.
        - Display cards using suit symbols: ♠ ♥ ♦ ♣ and show the card values clearly.
        - Always show the current score after each action.
        - After dealing, remind the player they can Hit or Stand.
        - When a game ends, show the outcome, payout, and updated balance.
        - If the player asks about their balance or history, use the appropriate tools.
        - Keep the tone fun and engaging — you're a dealer at a card table!
        - If the player runs out of money, let them know their balance is zero.
        """;

    public AIAgent Create() =>
        chatClient.AsAIAgent(
            options: new ChatClientAgentOptions
            {
                Name = AgentName,
                Description = AgentDescription,
                ChatOptions = new ChatOptions
                {
                    Instructions = Instructions,
                    Tools = [.. mcpTools]
                }
            });
}
