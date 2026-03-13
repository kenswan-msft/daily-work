using Aspire.Hosting.Azure;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIRECOSMOSDB001
IResourceBuilder<AzureCosmosDBResource> cosmos =
    builder.AddAzureCosmosDB("cosmos-db")
        .RunAsPreviewEmulator(emulator =>
        {
            emulator
                .WithDataExplorer()
                .WithDataVolume()
                .WithLifetime(ContainerLifetime.Persistent);
        });
#pragma warning restore ASPIRECOSMOSDB001

IResourceBuilder<AzureCosmosDBDatabaseResource> cosmosDatabase =
    cosmos.AddCosmosDatabase("conversations-db");

IResourceBuilder<AzureCosmosDBContainerResource> agentConversationContainer =
    cosmosDatabase.AddContainer("agent-conversations", "/conversationId");

builder.AddProject<Projects.DailyWork_Api>("dailywork-api")
    .WithReference(cosmosDatabase)
    .WithReference(agentConversationContainer)
    .WaitFor(cosmosDatabase);

builder.Build().Run();
