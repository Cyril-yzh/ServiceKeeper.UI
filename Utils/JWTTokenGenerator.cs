using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace ServiceKeeper.UI.Utils
{
    public static class JwtTokenGenerator
    {
        public static string GenerateToken(Dictionary<string, object> payload, byte[] secretKey,TimeSpan expirationTime)
        {
            var key = new SymmetricSecurityKey(secretKey);
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: GetClaims(payload),
                expires: DateTime.UtcNow.Add(expirationTime), //Token expiration time
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static IEnumerable<Claim> GetClaims(Dictionary<string, object> payload)
        {
            foreach (var kvp in payload)
            {
                yield return new Claim(kvp.Key, kvp.Value.ToString()!);
            }
        }

        public static bool IsValidJwtToken(string token, byte[] secretKey)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(secretKey);

            try
            {
                // 设置令牌验证参数
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = false, // 如果需要，您可以将它们设置为 true
                    ValidateAudience = false // 如果需要，您可以将它们设置为 true
                };

                //验证并解析令牌
                tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

                // 如果验证成功，并且令牌是有效的 Jwt 安全令牌，则返回 true
                return validatedToken is JwtSecurityToken;
            }
            catch (Exception)
            {
                // 如果 token 验证过程中出现异常，则返回 false
                return false;
            }
        }
    }
}