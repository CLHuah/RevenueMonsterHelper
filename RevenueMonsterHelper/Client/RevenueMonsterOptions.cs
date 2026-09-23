using RevenueMonsterLibrary.Constants;
using System;

namespace RevenueMonsterLibrary.Client;

/// <summary>
///     Revenue Monster environments.
/// </summary>
public enum RevenueMonsterEnvironment
{
    Sandbox,
    Production
}

/// <summary>
///     Settings for <see cref="RevenueMonsterClient" />.
/// </summary>
public sealed class RevenueMonsterOptions
{
    /// <summary>
    ///     The Open API base URL including the version, e.g. https://sb-open.revenuemonster.my/v3.
    /// </summary>
    internal string ApiBaseUrl =>
        $"{(Environment == RevenueMonsterEnvironment.Production ? RevenueMonsterUrls.ProductionApi : RevenueMonsterUrls.SandboxApi)}/{ApiVersion}";

    /// <summary>
    ///     The Open API version. Defaults to v3.
    /// </summary>
    public string ApiVersion { get; set; } = RevenueMonsterUrls.DefaultApiVersion;

    /// <summary>
    ///     The client ID from the Revenue Monster merchant portal.
    /// </summary>
    public string ClientId { get; set; }

    /// <summary>
    ///     The client secret from the Revenue Monster merchant portal.
    /// </summary>
    public string ClientSecret { get; set; }

    /// <summary>
    ///     The environment to call. Defaults to <see cref="RevenueMonsterEnvironment.Sandbox" />.
    /// </summary>
    public RevenueMonsterEnvironment Environment { get; set; } = RevenueMonsterEnvironment.Sandbox;

    /// <summary>
    ///     The OAuth API base URL including the version, e.g. https://sb-oauth.revenuemonster.my/v1.
    /// </summary>
    internal string OAuthBaseUrl =>
        $"{(Environment == RevenueMonsterEnvironment.Production ? RevenueMonsterUrls.ProductionOAuth : RevenueMonsterUrls.SandboxOAuth)}/{OAuthVersion}";

    /// <summary>
    ///     The OAuth API version. Defaults to v1.
    /// </summary>
    public string OAuthVersion { get; set; } = RevenueMonsterUrls.DefaultOAuthVersion;

    /// <summary>
    ///     Your RSA private key in PEM format, used to sign API requests. Its public key must be uploaded to the
    ///     Revenue Monster merchant portal.
    /// </summary>
    public string PrivateKey { get; set; }

    /// <summary>
    ///     How long before its expiry a cached access token is renewed. Defaults to 60 seconds.
    /// </summary>
    public TimeSpan TokenRenewalMargin { get; set; } = TimeSpan.FromSeconds(60);
}