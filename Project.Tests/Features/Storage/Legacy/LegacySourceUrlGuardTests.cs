using System.Net;
using Project.Features.Storage.Legacy;
using Xunit;

namespace Project.Tests.Features.Storage.Legacy;

public sealed class LegacySourceUrlGuardTests
{
    [Theory]
    [InlineData("andersonmultimarcas.com.br")]
    [InlineData("www.andersonmultimarcas.com.br")]
    public void IsAllowedHost_DeveAceitarSomenteDominiosConfigurados(string host)
    {
        var allowed = new[] { "andersonmultimarcas.com.br", "www.andersonmultimarcas.com.br" };

        Assert.True(LegacySourceUrlGuard.IsAllowedHost(host, allowed));
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("metadata.google.internal")]
    [InlineData("evil-andersonmultimarcas.com.br")]
    public void IsAllowedHost_DeveBloquearHostsNaoPermitidos(string host)
    {
        var allowed = new[] { "andersonmultimarcas.com.br", "www.andersonmultimarcas.com.br" };

        Assert.False(LegacySourceUrlGuard.IsAllowedHost(host, allowed));
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("10.0.0.5")]
    [InlineData("172.16.0.1")]
    [InlineData("192.168.1.10")]
    [InlineData("169.254.169.254")]
    public void IsBlockedAddress_DeveBloquearEnderecosPrivados(string address)
    {
        Assert.True(LegacySourceUrlGuard.IsBlockedAddress(IPAddress.Parse(address)));
    }

    [Fact]
    public void IsBlockedAddress_DevePermitirEnderecoPublico()
    {
        Assert.False(LegacySourceUrlGuard.IsBlockedAddress(IPAddress.Parse("8.8.8.8")));
    }
}
