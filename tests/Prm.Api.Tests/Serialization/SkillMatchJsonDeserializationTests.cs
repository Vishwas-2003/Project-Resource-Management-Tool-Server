using System.Text.Json;
using System.Text.Json.Serialization;

namespace Prm.Api.Tests.Serialization;

public class SkillMatchJsonDeserializationTests
{
    private sealed class SkillMatchResultItem
    {
        public int Rank { get; set; }

        public int ResourceUserId { get; set; }

        [JsonPropertyName("skills_match")]
        public string SkillsMatch { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void DeserializeResourceUserId_FromCamelCase()
    {
        const string json = """
            {
              "rank": 1,
              "resourceUserId": 3,
              "name": "Vijay",
              "skills_match": "C# (Advanced), .NET"
            }
            """;

        var item = JsonSerializer.Deserialize<SkillMatchResultItem>(json, JsonOptions);

        Assert.NotNull(item);
        Assert.Equal(3, item.ResourceUserId);
        Assert.Equal("C# (Advanced), .NET", item.SkillsMatch);
    }
}
