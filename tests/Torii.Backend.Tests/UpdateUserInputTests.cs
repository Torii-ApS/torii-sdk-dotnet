using System.Collections.Generic;
using Torii.Backend;
using Xunit;

namespace Torii.Backend.Tests;

public class UpdateUserInputTests
{
    [Fact]
    public void Set_with_value_includes_key()
    {
        var body = new UpdateUserInput { FirstName = Patch<string>.Set("Ada") }.ToJsonBody();
        Assert.True(body.ContainsKey("firstName"));
        Assert.Equal("\"Ada\"", body["firstName"]!.ToJsonString());
    }

    [Fact]
    public void Set_with_null_includes_key_as_null()
    {
        var body = new UpdateUserInput { LastName = Patch<string>.Set(null) }.ToJsonBody();
        Assert.True(body.ContainsKey("lastName"));
        Assert.Null(body["lastName"]);
    }

    [Fact]
    public void Omit_drops_key()
    {
        var body = new UpdateUserInput().ToJsonBody();
        Assert.False(body.ContainsKey("firstName"));
        Assert.False(body.ContainsKey("lastName"));
        Assert.False(body.ContainsKey("locale"));
        Assert.False(body.ContainsKey("unsafeMetadata"));
    }

    [Fact]
    public void Mixed_states_serialise_correctly()
    {
        var body = new UpdateUserInput
        {
            FirstName = Patch<string>.Set("Ada"),
            LastName = Patch<string>.Set(null),     // clear
            // Locale omitted
            UnsafeMetadata = Patch<IReadOnlyDictionary<string, object>>.Set(
                new Dictionary<string, object> { ["tier"] = "pro" }),
        }.ToJsonBody();

        var json = body.ToJsonString();
        Assert.Contains("\"firstName\":\"Ada\"", json);
        Assert.Contains("\"lastName\":null", json);
        Assert.DoesNotContain("locale", json);
        Assert.Contains("\"unsafeMetadata\":{\"tier\":\"pro\"}", json);
    }

    [Fact]
    public void Omit_singleton_is_default()
    {
        var input = new UpdateUserInput();
        Assert.True(input.FirstName.IsOmitted);
        Assert.True(input.LastName.IsOmitted);
        Assert.True(input.Locale.IsOmitted);
        Assert.True(input.UnsafeMetadata.IsOmitted);
    }
}
