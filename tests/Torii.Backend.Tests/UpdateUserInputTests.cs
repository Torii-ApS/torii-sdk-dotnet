using Torii.Backend;
using Xunit;

namespace Torii.Backend.Tests;

public class UpdateUserInputTests
{
    [Fact]
    public void Set_includes_key_with_value()
    {
        var body = new UpdateUserInput { Name = Patch<string>.Set("Ada") }.ToJsonBody();
        Assert.True(body.ContainsKey("name"));
        Assert.Equal("\"Ada\"", body["name"]!.ToJsonString());
    }

    [Fact]
    public void Clear_includes_key_as_null()
    {
        var body = new UpdateUserInput { Phone = Patch<string>.Clear() }.ToJsonBody();
        Assert.True(body.ContainsKey("phone"));
        Assert.Null(body["phone"]);
    }

    [Fact]
    public void Omit_drops_key()
    {
        var body = new UpdateUserInput().ToJsonBody();
        Assert.False(body.ContainsKey("name"));
        Assert.False(body.ContainsKey("phone"));
        Assert.False(body.ContainsKey("avatarUrl"));
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
            Phone = Patch<string>.Clear(),
            // AvatarUrl omitted
            Locale = Patch<string>.Set("en"),
            DateOfBirth = Patch<string>.Set("1990-07-15"),
        }.ToJsonBody();

        var json = body.ToJsonString();
        Assert.Contains("\"name\":\"Ada\"", json);
        Assert.Contains("\"phone\":null", json);
        Assert.DoesNotContain("avatarUrl", json);
        Assert.Contains("\"locale\":\"en\"", json);
        Assert.Contains("\"dateOfBirth\":\"1990-07-15\"", json);
    }

    [Fact]
    public void Omit_singleton_is_default()
    {
        var input = new UpdateUserInput();
        Assert.Equal(Patch<string>.State.Omitted, input.Name.Kind);
        Assert.Equal(Patch<string>.State.Omitted, input.Phone.Kind);
        Assert.Equal(Patch<string>.State.Omitted, input.AvatarUrl.Kind);
        Assert.Equal(Patch<string>.State.Omitted, input.Locale.Kind);
        Assert.Equal(Patch<string>.State.Omitted, input.Address.Kind);
        Assert.Equal(Patch<string>.State.Omitted, input.DateOfBirth.Kind);
    }
}
