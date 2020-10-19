using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace JwtAndRefreshTokenAuth.Services
{
    public class ColectionService
    {
        private readonly IConfigService _configService;
        public ColectionService(IConfigService configService)
        {
            _configService = configService;
        }
        public void AddJwtAuthentication(IServiceCollection services)
        {
            services
              .AddAuthentication(s =>
              {
                  //添加JWT Scheme
                  s.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                  s.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                  s.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
              })   // 檢查 HTTP Header 的 Authorization 是否有 JWT Bearer Token           
              .AddJwtBearer(options => //JWT驗證的參數
              {
                  options.TokenValidationParameters = new TokenValidationParameters
                  {
                      ValidateAudience = false,
                      ValidateIssuer = false,
                      ValidateLifetime = true,
                      ClockSkew = TimeSpan.Zero, //時間偏移容忍範圍，預設為300秒
                      ValidateIssuerSigningKey = true,
                      IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configService.GetJwtKey()))
                  };
                  options.Events = new JwtBearerEvents
                  {
                      OnAuthenticationFailed = context =>
                      {  //Token expired                          
                          if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                              context.Response.Headers.Add("Token-Expired", "true");
                          return Task.CompletedTask;
                      }
                  };
              });
        }
    }
}
