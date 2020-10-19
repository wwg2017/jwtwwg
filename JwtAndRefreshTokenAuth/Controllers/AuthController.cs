using JwtAndRefreshTokenAuth.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace JwtAndRefreshTokenAuth.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfigService _configService;
        private readonly IJwtService _jwtService;
        public AuthController(IConfigService configService, IJwtService jwtService)
        {
            _configService = configService;
            _jwtService = jwtService;
        }

        [HttpGet("GetToken")]
        public IActionResult GetToken()
        {
            return Ok(
                   new
                   {
                       Token = _jwtService.CreateJwtToken(
                           "Vic", new Dictionary<string, string>() { { "Phone", "09123456789" } }, _configService.GetJwtKey()
                          , TimeSpan.FromMinutes(_configService.GetJwtExpireTimeMinutes())
                          , TimeSpan.FromMinutes(_configService.GetJwtRefreshExpireTimeMinutes()), 
                           DateTime.Now.AddMinutes(_configService.GetJwtExpireTimeMinutes())
                          )
                   }
               );
        }

        [HttpGet("VerifyToken/{token}")]
        public IActionResult VerifyToken(string token)
        {
            var payload = default(Dictionary<string, object>);
            return Ok(
                    new
                    {
                        IsValid = _jwtService.VerifyJWT(token, _configService.GetJwtKey(), out payload),
                        payload
                    }
                );
        }
        /// <summary>
        /// 传递的是加密的token，然后里面包含refreshtoken的唯一标识
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet("RefreshToken/{token}")]
        public IActionResult RefreshToken(string token)
        {
            return Ok(
                    new
                    {
                        Token = _jwtService.RefreshToken(
                            token, _configService.GetJwtKey()
                            , TimeSpan.FromMinutes(_configService.GetJwtExpireTimeMinutes())
                          , TimeSpan.FromMinutes(_configService.GetJwtRefreshExpireTimeMinutes()),
                           DateTime.Now.AddMinutes(_configService.GetJwtExpireTimeMinutes())
                             )
                    }
                );
        }

    }
}