IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.DailyWork_Api>("dailywork-api");

builder.Build().Run();
