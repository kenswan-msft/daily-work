using DailyWork.Mcp.Obsidian.Configuration;
using DailyWork.Mcp.Obsidian.Services;
using DailyWork.Mcp.Obsidian.Tools;
using DailyWork.Mcp.Shared;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace DailyWork.Mcp.Obsidian.Test.Tools;

public class TaskToolsTests
{
    private readonly IObsidianCliService obsidianCli = Substitute.For<IObsidianCliService>();
    private readonly TaskTools sut;

    public TaskToolsTests()
    {
        IOptions<ObsidianOptions> options = Options.Create(new ObsidianOptions());
        sut = new TaskTools(obsidianCli, options, NullLogger<TaskTools>.Instance);
    }

    [Fact]
    public async Task ListTasks_Success_ReturnsTasks()
    {
        obsidianCli
            .ListTasksAsync(null, null, null, Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "[{\"path\":\"note.md\",\"line\":3,\"text\":\"Buy groceries\"}]", string.Empty));

        dynamic result = await sut.ListTasks(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Contains("Buy groceries", (string)result.Tasks);
    }

    [Fact]
    public async Task UpdateTask_ValidAction_ReturnsUpdated()
    {
        obsidianCli
            .UpdateTaskAsync("note.md", 3, "done", Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Updated", string.Empty));

        dynamic result = await sut.UpdateTask("note.md", 3, "done", TestContext.Current.CancellationToken);

        Assert.True((bool)result.Updated);
        Assert.Equal("done", (string)result.Action);
    }

    [Fact]
    public async Task UpdateTask_InvalidAction_ReturnsError()
    {
        dynamic result = await sut.UpdateTask("note.md", 3, "invalid", TestContext.Current.CancellationToken);

        Assert.Contains("Invalid action", (string)result.Error);
        await obsidianCli.DidNotReceive().UpdateTaskAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateTask_ToggleAction_IsValid()
    {
        obsidianCli
            .UpdateTaskAsync("note.md", 5, "toggle", Arg.Any<CancellationToken>())
            .Returns(new CliResult(0, "Toggled", string.Empty));

        dynamic result = await sut.UpdateTask("note.md", 5, "toggle", TestContext.Current.CancellationToken);

        Assert.True((bool)result.Updated);
    }
}
