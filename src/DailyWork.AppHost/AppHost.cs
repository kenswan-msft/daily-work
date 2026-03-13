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

IResourceBuilder<AzureCosmosDBContainerResource> conversationMetadataContainer =
    cosmosDatabase.AddContainer("conversation-metadata", "/id");

IResourceBuilder<ParameterResource> sqlPassword =
    builder.AddParameter(
        "sql-password",
        value: new GenerateParameterDefault
        {
            MinLength = 64,
            Special = false
        },
        secret: true,
        persist: true);

IResourceBuilder<SqlServerDatabaseResource> goalsDb =
    builder.AddSqlServer("dailywork-sql-server", password: sqlPassword)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume()
        .AddDatabase("goals-db");

IResourceBuilder<ProjectResource> goalsMcp =
    builder.AddProject<Projects.DailyWork_Mcp_Goals>("goals-mcp")
        .WithReference(goalsDb)
        .WaitFor(goalsDb);

builder.AddProject<Projects.DailyWork_Api>("dailywork-api")
    .WithReference(goalsMcp)
    .WithReference(cosmosDatabase)
    .WithReference(agentConversationContainer)
    .WithReference(conversationMetadataContainer)
    .WaitFor(cosmosDatabase)
    .WaitFor(goalsMcp);

builder.Build().Run();
