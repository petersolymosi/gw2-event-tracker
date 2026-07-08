using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Blish_HUD.Gw2WebApi;
using Blish_HUD.Modules.Managers;
using Gw2Sharp;
using Gw2Sharp.WebApi;
using Gw2Sharp.WebApi.Caching;

namespace Ghost.Gw2EventTracker.Services {

    /// <summary>
    /// Fetches account daily-progress endpoints through a module-owned Gw2Sharp client
    /// with <see cref="NullCacheMethod"/> so Blish HUD's shared in-memory cache is bypassed.
    /// </summary>
    internal sealed class AccountProgressApiFetcher {

        private static FieldInfo? ApiManagerConnectionField =>
            _apiManagerConnectionField ??= typeof(Gw2ApiManager).GetField(
                "_connection",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static FieldInfo? _apiManagerConnectionField;

        public static async Task<IReadOnlyList<string>> FetchWorldBossesAsync(
            Gw2ApiManager apiManager,
            CancellationToken cancellationToken = default) {
            using var client = CreateUncachedClient(apiManager);
            return await client.WebApi.V2.Account.WorldBosses.GetAsync(cancellationToken).ConfigureAwait(false);
        }

        public static async Task<IReadOnlyList<string>> FetchMapChestsAsync(
            Gw2ApiManager apiManager,
            CancellationToken cancellationToken = default) {
            using var client = CreateUncachedClient(apiManager);
            return await client.WebApi.V2.Account.MapChests.GetAsync(cancellationToken).ConfigureAwait(false);
        }

        internal static IConnection CreateUncachedConnection(IConnection blishConnection) {
            if (blishConnection == null) {
                throw new ArgumentNullException(nameof(blishConnection));
            }

            return new Connection(
                blishConnection.AccessToken,
                blishConnection.Locale,
                new NullCacheMethod(),
                new NullCacheMethod(),
                userAgent: blishConnection.UserAgent,
                httpClient: blishConnection.HttpClient);
        }

        internal static IConnection? ResolveBlishConnection(Gw2ApiManager apiManager) {
            if (ApiManagerConnectionField?.GetValue(apiManager) is ManagedConnection managed) {
                return managed.Connection;
            }

            return ResolveConnectionFromWebApiClient(apiManager.Gw2ApiClient);
        }

        private static Gw2Client CreateUncachedClient(Gw2ApiManager apiManager) {
            var blishConnection = ResolveBlishConnection(apiManager)
                ?? throw new InvalidOperationException(
                    "Could not resolve Gw2Sharp connection from Blish API manager.");

            if (string.IsNullOrWhiteSpace(blishConnection.AccessToken)) {
                throw new InvalidOperationException("GW2 API subtoken is not available.");
            }

            return new Gw2Client(CreateUncachedConnection(blishConnection));
        }

        private static IConnection? ResolveConnectionFromWebApiClient(IGw2WebApiClient webApiClient) {
            for (var type = webApiClient.GetType(); type != null; type = type.BaseType) {
                var property = type.GetProperty(
                    "Connection",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

                if (property?.GetValue(webApiClient) is IConnection connection) {
                    return connection;
                }
            }

            return null;
        }
    }

}
