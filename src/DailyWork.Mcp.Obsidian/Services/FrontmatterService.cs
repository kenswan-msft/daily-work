using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DailyWork.Mcp.Obsidian.Services;

public class FrontmatterService
{
    private static readonly IDeserializer Deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .Build();

    private static readonly ISerializer Serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    public (Dictionary<string, object>? Frontmatter, string Body) Parse(string content)
    {
        if (!content.StartsWith("---"))
        {
            return (null, content);
        }

        int endIndex = content.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (endIndex < 0)
        {
            return (null, content);
        }

        string yamlBlock = content[3..endIndex].Trim();
        string body = content[(endIndex + 4)..].TrimStart('\n', '\r');

        try
        {
            Dictionary<string, object>? frontmatter =
                Deserializer.Deserialize<Dictionary<string, object>>(yamlBlock);

            return (frontmatter ?? [], body);
        }
        catch
        {
            return (null, content);
        }
    }

    public string Compose(Dictionary<string, object>? frontmatter, string body)
    {
        if (frontmatter is null or { Count: 0 })
        {
            return body;
        }

        string yaml = Serializer.Serialize(frontmatter).TrimEnd('\n', '\r');
        return $"---\n{yaml}\n---\n{body}";
    }

    public string SetFields(string content, Dictionary<string, object> fieldsToSet)
    {
        (Dictionary<string, object>? existing, string body) = Parse(content);

        Dictionary<string, object> merged = existing ?? [];

        foreach (KeyValuePair<string, object> kvp in fieldsToSet)
        {
            merged[kvp.Key] = kvp.Value;
        }

        return Compose(merged, body);
    }
}
