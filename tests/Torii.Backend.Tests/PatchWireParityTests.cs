using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Torii.Backend;
using Torii.Backend.Generated.Model;
using Xunit;

namespace Torii.Backend.Tests;

/// <summary>
/// Wire-parity against the shared contract fixtures
/// (contract-tests/fixtures/patch-wire, vendored to the test output). For each
/// UpdateUserRequest fixture we round-trip expectedBody through the generated
/// model and the Patch-aware Newtonsoft settings the SDK uses to PATCH; the
/// result must match the blessed bytes (an absent key stays absent => leave, an
/// explicit null stays null => clear, nested nulls survive => key delete),
/// matching the server round-trip test and every other SDK.
/// </summary>
public class PatchWireParityTests
{
    public static IEnumerable<object[]> UpdateFixtures()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "patch-wire-fixtures.json");
        var manifest = JObject.Parse(File.ReadAllText(path));
        foreach (var fixture in manifest["fixtures"]!)
        {
            // The C# SDK's tri-state path is Users.UpdateAsync (UpdateUserRequest);
            // cover those fixtures here, as the PHP/Ruby SDKs do for their update path.
            if ((string?)fixture["schema"] == "UpdateUserRequest")
            {
                yield return new object[]
                {
                    (string)fixture["name"]!,
                    fixture["expectedBody"]!.ToString(Formatting.None),
                };
            }
        }
    }

    [Theory]
    [MemberData(nameof(UpdateFixtures))]
    public void SdkEmitsBlessedWireBytes(string name, string expectedBody)
    {
        var model = JsonConvert.DeserializeObject<UpdateUserRequest>(expectedBody, PatchSerialization.Settings);
        var wire = JsonConvert.SerializeObject(model, PatchSerialization.Settings);

        Assert.True(
            JToken.DeepEquals(JToken.Parse(wire), JToken.Parse(expectedBody)),
            $"fixture '{name}': expected {expectedBody}, got {wire}");
    }
}
