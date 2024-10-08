using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace WebAPI
{
    public class AuthTokenValidatorSso
    {
        private readonly SsoClient ssoClient;

        public AuthTokenValidatorSso(String ssoUri)
        {
            ssoClient = new SsoClient(ssoUri);
        }

        public async Task<bool> Validate(string token)
        {
            var jwt = DecodeToken(token);
            var keyIdStr = jwt.Header.Kid;

            if (string.IsNullOrEmpty(keyIdStr)) return false;
            if (!Guid.TryParse(keyIdStr, out var keyId)) return false;

            var publicKey = await GetKey(keyId);
            if (publicKey == null) return false;

            var securityKey = new RsaSecurityKey(publicKey.Value);
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

        private async Task<RSAParameters?> GetKey(Guid keyId)
        {
            try
            {
                var publicKeyResult = await ssoClient.GetPublicKeyAsync(keyId);
                var publicKeyData = publicKeyResult.result.PublicKey;

                using (var rsaProvider = KeyConverter.X509ToPublicKey(publicKeyData))
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