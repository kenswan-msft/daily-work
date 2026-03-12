using Microsoft.Agents.AI.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DailyWork.Agents;

public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAgenticChatClient()
        {
            services.AddOptions<ChatClientOptions>()
                .BindConfiguration(nameof(ChatClientOptions))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton(sp =>
            {
                ChatClientOptions options =
                    sp.GetRequiredService<IOptions<ChatClientOptions>>().Value;

                return ChatClientFactory.CreateChatClient(options);
            });

            return services;
        }

        public IHostedAgentBuilder AddAgentFactory<TFactory>()
            where TFactory : class, IAgentFactory
        {
            services.AddSingleton<TFactory>();

            return services.AddAIAgent(
                TFactory.AgentName,
                (sp, _) => sp.GetRequiredService<TFactory>().Create());
        }
    }
}
