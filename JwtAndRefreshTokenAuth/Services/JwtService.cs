using JwtAndRefreshTokenAuth.Cache;
using JwtAndRefreshTokenAuth.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace JwtAndRefreshTokenAuth
{
    public class JwtService : IJwtService
    {
        private readonly IConfigService _configService;
        private static IDistributedCache _distributedCache;
        private const string RefreshTokenName = "RefreshToken";
        private readonly RedisAllotStance _stance = null;
        public JwtService(IConfigService configService, RedisAllotStance stance)
        {
            _configService = configService;
            _stance = stance;
        }

        //public JwtService(IConfigService configService, IDistributedCache distributedCache, RedisAllotStance stance)
        //{
        //    _configService = configService;
        //    _distributedCache = distributedCache;
        //    _stance = stance;
        //}

        public string CreateJwtToken(string userName, Dictionary<string, string> payload, string signKey, TimeSpan expireTime, TimeSpan refreshExpireTime, DateTime expireDatetime)
        {

            _stance.RedisStanceBase().HashSet("order_hashkey", "order_hashfield", "10");
            List<Person> Personlist = new List<Person>();
            Person person = new Person();
            person.Age = "2";
            person.Names = "wwg";
            Personlist.Add(person);

            Person person2 = new Person();
            person2.Age = "2";
            person2.Names = "wwg";
            Personlist.Add(person2);


            var jsondemp = JsonConvert.SerializeObject(Personlist);
            _stance.RedisStanceBase().HashSet("wwg2", "listr", jsondemp);

            _stance.RedisStanceBase().StringSet("wwg2ss", jsondemp);


            _stance.RedisStanceBase().HashSet("wwg2person", person.TohashEntries());
            Dictionary<int, string> keyValuePairs = new Dictionary<int, string>();
            keyValuePairs.Add(1, "1");
            keyValuePairs.Add(2, "2");

            var fields = keyValuePairs.Select(
    pair => new HashEntry(pair.Key, pair.Value)).ToArray();
            _stance.RedisStanceBase().HashSet("fields", fields);

            HashEntry[] hashEntries = _stance.RedisStanceBase().HashGetAll("fields");
            foreach (var item in hashEntries)
            {

            }
            var personobj = hashEntries.ConvertFromRedis<Person>();
            return "";
            if (string.IsNullOrEmpty(signKey) || signKey.Length < 16)
                throw new Exception("'signKey' must >= 16 chars");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            if (payload == null)
                payload = new Dictionary<string, string>();

            if (refreshExpireTime != null)
            {
                var refreshToken = Guid.NewGuid().ToString("N");
                AddRefreshTokenToCache(refreshToken, refreshExpireTime);

                payload[RefreshTokenName] = refreshToken;
            }

            payload[JwtRegisteredClaimNames.Sub] = userName;
            var claims = payload.Select(x => new Claim(x.Key, x.Value)).ToList();

            var token = new JwtSecurityToken(
                claims: claims,
                expires: expireDatetime,
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public bool VerifyJWT(string token, string signKey, out Dictionary<string, object> outPayload)
        {
            outPayload = new Dictionary<string, object>();

            //檢查簽章是否正確
            ClaimsPrincipal cp;
            try
            {
                SecurityToken validatedToken;
                cp = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = false,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signKey))
                }, out validatedToken);

                //解 Payload                   
                outPayload = cp.Claims.ToDictionary(x => x.Type, y => (object)y.Value);
            }
            catch
            {
                //Incorrect JWT format
                cp = null;
            }
            return cp != null;
        }

        private static object LockRefreshTokenCache = new object();
        public string RefreshToken(string token, string signKey, TimeSpan expireTime, TimeSpan refreshExpireTime, DateTime expireDatetime)
        {
            var newToken = "";
            try
            {
                string decodedString = GetJwtPayloadStringFromBase64(token);
                if (string.IsNullOrEmpty(decodedString))
                    return newToken;

                var payloadDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(decodedString);

                if (payloadDic.ContainsKey(RefreshTokenName))
                {
                    var currentRefreshKey = payloadDic[RefreshTokenName];

                    //avoid token refresh multiple times
                    lock (LockRefreshTokenCache)
                    {
                        //check refresh token
                        if (!IsExistsRefreshTokenCache(currentRefreshKey))
                            return newToken;

                        //create new token
                        newToken = CreateJwtToken(payloadDic[JwtRegisteredClaimNames.Sub], payloadDic, signKey, expireTime, refreshExpireTime, expireDatetime);

                        //delete current refresh token 
                        //RemoveRefreshTokenCache(currentRefreshKey);
                        //_stance.RedisStanceBase().KeyDelete(currentRefreshKey);
                    }
                }
            }
            catch (Exception e)
            {
                //todo: write log
            }
            return newToken;
        }
        private string GetJwtPayloadStringFromBase64(string token)
        {
            var base64Payload = token.Split('.')[1];
            var appendCount = 4 - (base64Payload.Length % 4);
            if (appendCount > 0 && appendCount < 4)
                base64Payload += string.Concat(Enumerable.Repeat('=', appendCount));

            byte[] data = Convert.FromBase64String(base64Payload);
            return Encoding.UTF8.GetString(data);
        }



        private void AddRefreshTokenToCache(string refreshToken, TimeSpan expireTime)
        {
            _stance.RedisStanceBase().StringSet(GetRefreshTokenCacheName(refreshToken), expireTime.ToString(), expireTime);
        }
        private bool IsExistsRefreshTokenCache(string refreshToken)
        {
            return !string.IsNullOrEmpty(_stance.RedisStanceBase().StringGet(GetRefreshTokenCacheName(refreshToken)));
        }

        private void RemoveRefreshTokenCache(string refreshToken)
            => _distributedCache.Remove(GetRefreshTokenCacheName(refreshToken));

        private string GetRefreshTokenCacheName(string refreshToken)
            => $"Cache:{RefreshTokenName}ExpireTime:{refreshToken}";

        private DistributedCacheEntryOptions GetdistributedCacheEntryOptions(DateTime expireTime)
            => new DistributedCacheEntryOptions { AbsoluteExpiration = expireTime };
    }
}
