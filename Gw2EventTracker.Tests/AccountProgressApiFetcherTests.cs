using System;
using System.Threading;
using System.Threading.Tasks;
using Ghost.Gw2EventTracker.Services;
using Gw2Sharp;
using Gw2Sharp.WebApi;
using Gw2Sharp.WebApi.Caching;
using Gw2Sharp.WebApi.Http;
using Gw2Sharp.WebApi.Middleware;
using Xunit;

namespace Ghost.Gw2EventTracker.Tests {

    public sealed class AccountProgressApiFetcherTests {

        [Fact]
        public void CreateUncachedConnection_CopiesTokenLocaleAndHttpClient_UsesNullCache() {
            var blishConnection = new Connection("test-subtoken", Locale.English);

            var uncached = AccountProgressApiFetcher.CreateUncachedConnection(blishConnection);

            Assert.Equal("test-subtoken", uncached.AccessToken);
            Assert.Equal(Locale.English, uncached.Locale);
            Assert.Same(blishConnection.HttpClient, uncached.HttpClient);
            Assert.IsType<NullCacheMethod>(uncached.CacheMethod);
            Assert.IsType<NullCacheMethod>(uncached.RenderCacheMethod);
        }

        [Fact]
        public void CreateUncachedConnection_ThrowsWhenSourceIsNull() {
            Assert.Throws<ArgumentNullException>(
                () => AccountProgressApiFetcher.CreateUncachedConnection(null!));
        }

        [Fact]
        public void AttachBlishRateLimitMiddleware_CopiesTokenCompliance_SkipsCacheMiddleware() {
            var source = new Connection("token", Locale.English);
            var target = new Connection("token", Locale.English, new NullCacheMethod());
            var rateLimit = new TokenComplianceMiddleware();
            var cache = new CacheMiddleware();

            source.Middleware.Add(cache);
            source.Middleware.Add(rateLimit);

            var attached = AccountProgressApiFetcher.AttachBlishRateLimitMiddleware(source, target);

            Assert.Equal(1, attached);
            Assert.Contains(rateLimit, target.Middleware);
            Assert.DoesNotContain(cache, target.Middleware);
        }

        private sealed class TokenComplianceMiddleware : IWebApiMiddleware {
            public Task<IWebApiResponse> OnRequestAsync(
                MiddlewareContext context,
                Func<MiddlewareContext, CancellationToken, Task<IWebApiResponse>> callNext,
                CancellationToken cancellationToken = default) =>
                callNext(context, cancellationToken);
        }
    }

}
