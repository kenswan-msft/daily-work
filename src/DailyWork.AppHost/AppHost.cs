IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

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

IResourceBuilder<SqlServerServerResource> sqlServer =
    builder.AddSqlServer("dailywork-sql-server", password: sqlPassword, port: 63141)
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume();

IResourceBuilder<SqlServerDatabaseResource> goalsDb =
    sqlServer.AddDatabase("goals-db");

IResourceBuilder<SqlServerDatabaseResource> blackjackDb =
    sqlServer.AddDatabase("blackjack-db");

IResourceBuilder<SqlServerDatabaseResource> knowledgeDb =
    sqlServer.AddDatabase("knowledge-db");

IResourceBuilder<SqlServerDatabaseResource> filesystemDb =
    sqlServer.AddDatabase("filesystem-db");

IResourceBuilder<SqlServerDatabaseResource> projectsDb =
    sqlServer.AddDatabase("projects-db");

IResourceBuilder<SqlServerDatabaseResource> conversationsDb =
    sqlServer.AddDatabase("conversations-db");

IResourceBuilder<ProjectResource> goalsMcp =
    builder.AddProject<Projects.DailyWork_Mcp_Goals>("goals-mcp")
        .WithReference(goalsDb)
        .WaitFor(goalsDb);

IResourceBuilder<ProjectResource> blackjackMcp =
    builder.AddProject<Projects.DailyWork_Mcp_Blackjack>("blackjack-mcp")
        .WithReference(blackjackDb)
        .WaitFor(blackjackDb);

IResourceBuilder<ProjectResource> knowledgeMcp =
    builder.AddProject<Projects.DailyWork_Mcp_Knowledge>("knowledge-mcp")
        .WithReference(knowledgeDb)
        .WaitFor(knowledgeDb);

IResourceBuilder<ProjectResource> filesystemMcp =
    builder.AddProject<Projects.DailyWork_Mcp_FileSystem>("filesystem-mcp")
        .WithReference(filesystemDb)
        .WaitFor(filesystemDb);

IResourceBuilder<ProjectResource> projectsMcp =
    builder.AddProject<Projects.DailyWork_Mcp_Projects>("projects-mcp")
        .WithReference(projectsDb)
        .WaitFor(projectsDb);

IResourceBuilder<ProjectResource> githubMcp =
    builder.AddProject<Projects.DailyWork_Mcp_GitHub>("github-mcp");

IResourceBuilder<ProjectResource> dotnetMcp =
    builder.AddProject<Projects.DailyWork_Mcp_DotNet>("dotnet-mcp");

IResourceBuilder<ProjectResource> api =
    builder.AddProject<Projects.DailyWork_Api>("dailywork-api")
    .WithReference(goalsDb)
    .WithReference(knowledgeDb)
    .WithReference(projectsDb)
    .WithReference(goalsMcp)
    .WithReference(blackjackMcp)
    .WithReference(knowledgeMcp)
    .WithReference(filesystemMcp)
    .WithReference(projectsMcp)
    .WithReference(githubMcp)
    .WithReference(dotnetMcp)
    .WithReference(conversationsDb)
    .WaitFor(conversationsDb)
    .WaitFor(goalsMcp)
    .WaitFor(blackjackMcp)
    .WaitFor(knowledgeMcp)
    .WaitFor(filesystemMcp)
    .WaitFor(projectsMcp)
    .WaitFor(githubMcp)
    .WaitFor(dotnetMcp);

builder.AddProject<Projects.DailyWork_Web>("dailywork-web")
    .WithReference(api)
    .WithExternalHttpEndpoints()
    .WaitFor(api);

builder.Build().Run();
