using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;

namespace JwtAndRefreshTokenAuth.Cache
{
    public class RedisAllotStance
    {
        private IConfiguration _IConfiguration = null;
        public RedisAllotStance(IConfiguration IConfiguration)
        {
            _IConfiguration = IConfiguration;
        }
        public IDatabase RedisStanceBase()
        {
            return RedisClientSingleton.GetInstance(_IConfiguration).GetDatabase("Redis_Default");
        }
    }
}
