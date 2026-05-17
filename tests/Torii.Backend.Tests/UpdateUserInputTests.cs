using Torii.Backend;
using Xunit;

namespace Torii.Backend.Tests;

public class UpdateUserInputTests
{
    [Fact]
    public void Set_with_value_includes_key()
    {
        var body = new UpdateUserInput { Name = Patch<string>.Set("Ada") }.ToJsonBody();
        Assert.True(body.ContainsKey("name"));
        Assert.Equal("\"Ada\"", body["name"]!.ToJsonString());
    }

    [Fact]
    public void Set_with_null_includes_key_as_null()
    {
        var body = new UpdateUserInput { Phone = Patch<string>.Set(null) }.ToJsonBody();
        Assert.True(body.ContainsKey("phone"));
        Assert.Null(body["phone"]);
    }

    [Fact]
    public void Omit_drops_key()
    {
        var body = new UpdateUserInput().ToJsonBody();
        Assert.False(body.ContainsKey("name"));
        Assert.False(body.ContainsKey("phone"));
        Assert.False(body.ContainsKey("locale"));
        Assert.False(body.ContainsKey("address"));
        Assert.False(body.ContainsKey("dateOfBirth"));
    }

    [Fact]
    public void Mixed_states_serialise_correctly()
    {
        var body = new UpdateUserInput
        {
            Name = Patch<string>.Set("Ada"),
            Phone = Patch<string>.Set(null),     // clear
            // Address omitted
            Locale = Patch<string>.Set("en"),
            DateOfBirth = Patch<string>.Set("1990-07-15"),
        }.ToJsonBody();

        var json = body.ToJsonString();
        Assert.Contains("\"name\":\"Ada\"", json);
        Assert.Contains("\"phone\":null", json);
        Assert.DoesNotContain("address", json);
        Assert.Contains("\"locale\":\"en\"", json);
        Assert.Contains("\"dateOfBirth\":\"1990-07-15\"", json);
    }

    [Fact]
    public void Omit_singleton_is_default()
    {
        var input = new UpdateUserInput();
        Assert.True(input.Name.IsOmitted);
        Assert.True(input.Phone.IsOmitted);
        Assert.True(input.Locale.IsOmitted);
        Assert.True(input.Address.IsOmitted);
        Assert.True(input.DateOfBirth.IsOmitted);
    }
}
