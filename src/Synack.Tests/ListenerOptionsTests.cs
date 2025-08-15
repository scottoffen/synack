using System.Net;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using Synack.Authentication;
using Synack.Diagnostics;
using Synack.Exceptions;

namespace Synack.Tests;

public class ListenerOptionsTests
{
    [Fact]
    public void Defaults_AreExpected_WhenConstructed()
    {
        var o = new ListenerOptions();

        o.BindAddress.ShouldBe(IPAddress.Any);
        o.Port.ShouldBe(0);
        o.TlsEnabled.ShouldBeFalse();
        o.Prefixes.Count.ShouldBe(0);
        o.TraceIdentifierProvider.ShouldBeOfType<GuidTraceIdentifierProvider>();
        o.IsSealed.ShouldBeFalse();
    }

    [Fact]
    public void BindAddress_Throws_WhenSealed()
    {
        var o = new ListenerOptions { BindAddress = IPAddress.Loopback };
        o.Seal();

        var ex = Should.Throw<InvalidOperationException>(() => o.BindAddress = IPAddress.Any);
        ex.Message.ShouldBe(ListenerOptions.MessageOptionsAreSealed);
    }

    [Fact]
    public void BindAddress_Throws_WhenNull()
    {
        var o = new ListenerOptions();

        var ex = Should.Throw<ArgumentNullException>(() => o.BindAddress = null!);
        ex.ParamName.ShouldBe("value");
        ex.Message.ShouldContain(ListenerOptions.MessageBindAddressCannotBeNull, Case.Insensitive);
    }

    [Fact]
    public void Port_Throws_WhenOutOfRange()
    {
        var o = new ListenerOptions();

        var exNeg = Should.Throw<ArgumentOutOfRangeException>(() => o.Port = -1);
        exNeg.ParamName.ShouldBe("value");
        exNeg.Message.ShouldContain(ListenerOptions.MessagePortOutOfRange, Case.Insensitive);

        var exBig = Should.Throw<ArgumentOutOfRangeException>(() => o.Port = 70000);
        exBig.ParamName.ShouldBe("value");
        exBig.Message.ShouldContain(ListenerOptions.MessagePortOutOfRange, Case.Insensitive);
    }

    [Fact]
    public void Port_Throws_WhenSealed()
    {
        var o = new ListenerOptions { Port = 5000 };
        o.Seal();

        var ex = Should.Throw<InvalidOperationException>(() => o.Port = 5001);
        ex.Message.ShouldBe(ListenerOptions.MessageOptionsAreSealed);
    }

    [Fact]
    public void CheckCertificateRevocation_Throws_WhenSealed()
    {
        var o = new ListenerOptions { CheckCertificateRevocation = false };
        o.Seal();

        var ex = Should.Throw<InvalidOperationException>(() => o.CheckCertificateRevocation = true);
        ex.Message.ShouldBe(ListenerOptions.MessageOptionsAreSealed);
    }

    [Fact]
    public void RequireClientCertificate_Throws_WhenSealed()
    {
        var o = new ListenerOptions { RequireClientCertificate = false };
        o.Seal();

        var ex = Should.Throw<InvalidOperationException>(() => o.RequireClientCertificate = true);
        ex.Message.ShouldBe(ListenerOptions.MessageOptionsAreSealed);
    }

    [Fact]
    public void Seal_SetsIsSealed_AndUnseal_ClearsIt()
    {
        var o = new ListenerOptions();

        o.IsSealed.ShouldBeFalse();
        o.Seal();
        o.IsSealed.ShouldBeTrue();
        o.Unseal();
        o.IsSealed.ShouldBeFalse();
    }

    [Fact]
    public void RuntimeMutableProperties_CanChange_WhenSealed()
    {
        var o = new ListenerOptions();
        o.Seal();

        IAuthenticationHandler? ah = new DummyAuthHandler();
        o.AuthenticationHandler = ah;
        o.AuthenticationHandler.ShouldBeSameAs(ah);

        o.TraceIdentifierProvider = new DummyTraceIdProvider("abc");
        o.TraceIdentifierProvider.ShouldBeOfType<DummyTraceIdProvider>();

        o.ClientCertificateValidator = c => true;
        o.ClientCertificateValidator!.Invoke(TestCertificateFactory.Create("test")).ShouldBeTrue();

        o.Prefixes = new[] { "/one", "/TWO/" };
        o.AddPrefix("/Three");
        o.Prefixes.ShouldContain("/one/");
        o.Prefixes.ShouldContain("/two/");
        o.Prefixes.ShouldContain("/three/");
    }

    [Fact]
    public void Certificate_Throws_InvalidCertificateException_WhenCertificateMissingPrivateKey()
    {
        var o = new ListenerOptions();

        var withKey = TestCertificateFactory.Create("localhost");        // valid PFX w/ private key
        var noKey  = new X509Certificate2(withKey.Export(X509ContentType.Cert)); // strip private key

        var ex = Should.Throw<InvalidCertificateException>(() => o.Certificate = noKey);
        ex.Issues.ShouldNotBeEmpty(); // if your exception exposes Issues
        o.TlsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Certificate_SetsTlsEnabledTrue_WhenValid_AndFalse_WhenSetToNull()
    {
        var o = new ListenerOptions();

        var valid = TestCertificateFactory.Create("localhost");
        o.Certificate = valid;
        o.TlsEnabled.ShouldBeTrue();

        o.Certificate = null;
        o.TlsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void Prefixes_NormalizeAndDeduplicate_OnSet()
    {
        var o = new ListenerOptions
        {
            Prefixes = new[] { "api", "/API", "/api//", "/a/./b/../c" }
        };

        o.Prefixes.Count.ShouldBe(2);
        o.Prefixes.ShouldContain("/api/");
        o.Prefixes.ShouldContain("/a/c/");
    }

    [Fact]
    public void AddPrefix_Normalizes_IsIdempotent_AndRemovePrefix_IsCaseInsensitive()
    {
        var o = new ListenerOptions();

        o.AddPrefix("api");
        o.AddPrefix("/API/");
        o.AddPrefix(@"\api\\");
        o.Prefixes.Count.ShouldBe(1);
        o.Prefixes.ShouldContain("/api/");

        o.RemovePrefix("/ApI");
        o.Prefixes.Count.ShouldBe(0);
    }

    [Fact]
    public void Prefixes_Setter_AtomicallyReplacesSet_WhileRunning()
    {
        var o = new ListenerOptions { Prefixes = new[] { "/one/", "/two/" } };
        var first = o.Prefixes;

        o.Prefixes = new[] { "/two/", "/three" };
        var second = o.Prefixes;

        // old snapshot remains valid and independent
        first.ShouldContain("/one/");
        first.ShouldContain("/two/");
        first.ShouldNotContain("/three/");

        // new snapshot visible to readers
        second.ShouldContain("/two/");
        second.ShouldContain("/three/");
        second.ShouldNotContain("/one/");
    }

    [Fact]
    public async Task Prefixes_Enumeration_IsStable_DuringConcurrentUpdates()
    {
        var o = new ListenerOptions { Prefixes = new[] { "/a/", "/b/", "/c/" } };
        var snapshot = o.Prefixes;

        var iterateTask = Task.Run(() =>
        {
            var seen = new List<string>();
            foreach (var p in snapshot) seen.Add(p);
            return seen;
        });

        var updaterTask = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++) o.AddPrefix($"/x{i}/");
        });

        await Task.WhenAll(iterateTask, updaterTask);

        var x = await iterateTask;
        x.ShouldBeSubsetOf(snapshot);
        x.Count.ShouldBe(snapshot.Count);
    }

    private sealed class DummyAuthHandler : IAuthenticationHandler
    {
        public Task<ClaimsPrincipal> AuthenticateAsync(IHttpContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ClaimsPrincipal(new ClaimsIdentity("Dummy")));
        }
    }

    private sealed record DummyTraceIdProvider(string Value) : ITraceIdentifierProvider
    {
        public string GenerateTraceIdentifier()
        {
            return Value;
        }
    }
}
