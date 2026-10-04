using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace Spirometry.Web.Services
{
    public sealed class DatabaseSessionStore
    {
        private readonly IMemoryCache _cache;//önbellek tutuar

        public static readonly TimeSpan SessionLigeTime = TimeSpan.FromMinutes(20);



        public DatabaseSessionStore(IMemoryCache cache)
        {
            _cache = cache;

        }


        public string Create(DatabaseSession session)
        {
            string sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));


            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = SessionLigeTime//kullanıcı işlem yapsada süre uzamicak
            };
            _cache.Set(GetKey(sessionId), session, options);

            return sessionId;
        }

        public DatabaseSession? Get(string? sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return null;
            }

            return _cache.Get<DatabaseSession>(GetKey(sessionId));
        }


        public void Remove(string? sessionId)//çıkış yapınca önbellekten kaldır
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return;
            }
            _cache.Remove(GetKey(sessionId));
        }


        private static string GetKey(string sessionId)
        {
            return $"database-session:{sessionId}";
        }
    }
}
