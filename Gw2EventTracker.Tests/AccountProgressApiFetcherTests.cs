using Ghost.Gw2EventTracker.Services;
using Gw2Sharp;
using Gw2Sharp.WebApi;
using Gw2Sharp.WebApi.Caching;
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
            Assert.Throws<System.ArgumentNullException>(
                () => AccountProgressApiFetcher.CreateUncachedConnection(null!));
        }
    }

}
