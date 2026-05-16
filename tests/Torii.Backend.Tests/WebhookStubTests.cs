using System.Collections.Generic;
using Xunit;

namespace Torii.Backend.Tests;

public sealed class WebhookStubTests
{
    [Fact]
    public void Verify_webhook_throws_until_subsystem_ships()
    {
        var ex = Assert.Throws<ToriiAuthException>(() =>
            WebhookVerifier.VerifyWebhook("secret", new Dictionary<string, string>(), "{}"));
        Assert.Contains("webhook", ex.Message);
    }
}
