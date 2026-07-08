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
using Gw2Sharp.WebApi.Middleware;

namespace Ghost.Gw2EventTracker.Services {

    /// <summary>
    /// Fetches account daily-progress endpoints through a module-owned Gw2Sharp client
    /// with <see cref="NullCacheMethod"/> so Blish HUD's shared in-memory cache is bypassed.
    /// Uses Blish's <see cref="ManagedConnection"/> subtoken (token delegation) per
    /// https://blishhud.com/docs/modules/guides/gw2api/
    /// </summary>
    internal sealed class AccountProgressApiFetcher {

        private const string TokenComplianceMiddlewareName = "TokenComplianceMiddleware";

        private static FieldInfo? ApiManagerConnectionField =>
            _apiManagerConnectionField ??= typeof(Gw2ApiManager).GetField(
                "_connection",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private static FieldInfo? _apiManagerConnectionField;

        internal sealed class FetchResult {
            public IReadOnlyList<string> WorldBosses { get; set; } = Array.Empty<string>();
            public IReadOnlyList<string> MapChests { get; set; } = Array.Empty<string>();
            public bool WorldBossFailed { get; set; }
            public bool MapChestFailed { get; set; }
            public string? WorldBossError { get; set; }
            public string? MapChestError { get; set; }
        }

        public static async Task<FetchResult> FetchProgressAsync(
            Gw2ApiManager apiManager,
            CancellationToken cancellationToken = default) {
            using var client = CreateUncachedClient(apiManager);

            IReadOnlyList<string> worldBosses = Array.Empty<string>();
            IReadOnlyList<string> mapChests = Array.Empty<string>();
            string? worldBossError = null;
            string? mapChestError = null;
            var worldBossFailed = false;
            var mapChestFailed = false;

            try {
                worldBosses = await client.WebApi.V2.Account.WorldBosses
                    .GetAsync(cancellationToken)
                    .ConfigureAwait(false);
            } catch (Exception ex) {
                worldBossFailed = true;
                worldBossError = ex.Message;
            }

            try {
                mapChests = await client.WebApi.V2.Account.MapChests
                    .GetAsync(cancellationToken)
                    .ConfigureAwait(false);
            } catch (Exception ex) {
                mapChestFailed = true;
                mapChestError = ex.Message;
            }

            return new FetchResult {
                WorldBosses = worldBosses,
                MapChests = mapChests,
                WorldBossFailed = worldBossFailed,
                MapChestFailed = mapChestFailed,
                WorldBossError = worldBossError,
                MapChestError = mapChestError
            };
        }

        internal static IConnection CreateUncachedConnection(IConnection blishConnection) {
            if (blishConnection == null) {
                throw new ArgumentNullException(nameof(blishConnection));
            }

            var uncached = new Connection(
                blishConnection.AccessToken,
                blishConnection.Locale,
                new NullCacheMethod(),
                new NullCacheMethod(),
                userAgent: blishConnection.UserAgent,
                httpClient: blishConnection.HttpClient);

            AttachBlishRateLimitMiddleware(blishConnection, uncached);
            return uncached;
        }

        /// <summary>
        /// Shares Blish's token-bucket middleware so uncached requests respect the global rate limit.
        /// </summary>
        internal static int AttachBlishRateLimitMiddleware(IConnection source, IConnection target) {
            var attached = 0;

            foreach (var middleware in source.Middleware) {
                if (middleware is CacheMiddleware) {
                    continue;
                }

                if (!string.Equals(
                        middleware.GetType().Name,
                        TokenComplianceMiddlewareName,
                        StringComparison.Ordinal)) {
                    continue;
                }

                target.Middleware.Add(middleware);
                attached++;
            }

            return attached;
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
