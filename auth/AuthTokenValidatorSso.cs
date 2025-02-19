using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using WebAPI.auth;

namespace WebAPI
{
    public class AuthTokenValidatorSso
    {
        private readonly SsoClient _ssoClient;
        private readonly ConcurrentDictionary<Guid, string> _publicKeys = new ConcurrentDictionary<Guid, string>();

        public AuthTokenValidatorSso(SsoClient ssoClient)
        {
            _ssoClient = ssoClient;
        }

        public async Task<bool> Validate(string token)
        {
            var jwt = DecodeToken(token);
            var keyIdStr = jwt.Header.Kid;

            if (string.IsNullOrEmpty(keyIdStr)) return false;
            if (!Guid.TryParse(keyIdStr, out var keyId)) return false;


            if (!_publicKeys.TryGetValue(keyId, out var publicKey))
            {
                publicKey = await GetKey(keyId);
                if (publicKey == null) return false;
                _publicKeys.TryAdd(keyId, publicKey);
            }

            RSAParameters? rsaParameters = GetRsaParameters(publicKey);
            var securityKey = new RsaSecurityKey(rsaParameters.Value);
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidateAudience = false,

                IssuerSigningKey = securityKey,
                ValidIssuer = "ch.sncag.sso",
                ClockSkew = TimeSpan.FromSeconds(60)
            };
            var handler = new JwtSecurityTokenHandler();
            
            try
            {
                var claimsPrincipal = handler.ValidateToken(token, tokenValidationParameters, out SecurityToken validatedToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Token validation failed: " + ex.Message);
                return false;
            }
    
            return true;
        }

        private JwtSecurityToken DecodeToken(String token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadToken(token) as JwtSecurityToken;
            return jwtToken;
        }

        private async Task<string> GetKey(Guid keyId)
        {
            try
            {
                var publicKeyResult = await _ssoClient.GetPublicKeyAsync(keyId);
                return publicKeyResult.result.PublicKey;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            return null;
        }

        private RSAParameters? GetRsaParameters(string publicKey)
        {
            try
            {
                using (var rsaProvider = KeyConverter.X509ToPublicKey(publicKey))
                {
                    var rsaParameters = rsaProvider.ExportParameters(false);
                    return rsaParameters;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            return null;
        }

        private RSACryptoServiceProvider X509ToPublicKey(string publicKey)
        {
            var keyBytes = Convert.FromBase64String(publicKey);
            var cert = new X509Certificate2(keyBytes);
            var rsaProvider = (RSACryptoServiceProvider)cert.PublicKey.Key;
            return rsaProvider;
        }
    }

    public class KeyConverter
    {
        private static readonly Lazy<Base64Decoder> decoder = new(() => new Base64Decoder());
        
        private static RSA CreateRSAProviderFromPublicKey(string publicKey)
        {
            byte[] keyBytes = decoder.Value.Decode(publicKey);
            
            var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);

            return rsa;
        }
        
        public static RSA X509ToPublicKey(string publicKey)
        {
            return CreateRSAProviderFromPublicKey(publicKey);
        }
        
        private class Base64Decoder
        {
            public byte[] Decode(string base64String)
            {
                return Convert.FromBase64String(base64String);
            }
        }
    }
}