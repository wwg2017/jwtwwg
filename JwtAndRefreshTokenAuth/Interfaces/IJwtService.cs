using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace JwtAndRefreshTokenAuth
{
    public interface IJwtService
    {        
        string CreateJwtToken(string userName, Dictionary<string, string> payload, string signKey, TimeSpan expireTime, TimeSpan refreshExpireTime,DateTime expireDatetime);
        string RefreshToken(string token, string signKey, TimeSpan expireTime, TimeSpan refreshExpireTime,DateTime expireDatetime);
        bool VerifyJWT(string token, string signKey, out Dictionary<string, object> outPayload);
    }
}