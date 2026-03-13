namespace DailyWork.Cli;

public class ConsoleChatInputReader : IChatInputReader
{
    public string? ReadInput() => Console.ReadLine();
}
