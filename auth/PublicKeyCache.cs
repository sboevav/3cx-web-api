using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Threading.Tasks;
using WebAPI.auth;

public class PublicKeyCache
{
    private readonly SsoClient _ssoClient;
    private readonly ConcurrentDictionary<Guid, string> _publicKeys = new ConcurrentDictionary<Guid, string>();

    public PublicKeyCache(SsoClient ssoClient)
    {
        _ssoClient = ssoClient;
    }

    public async Task<RSAParameters> GetPublicKeyAsync(Guid keyId)
    {
        if (!_publicKeys.TryGetValue(keyId, out var publicKey))
        {
            var response = await _ssoClient.GetPublicKeyAsync(keyId);
            publicKey = response.result.PublicKey;
            _publicKeys.TryAdd(keyId, publicKey);
        }

        return FromXmlString(publicKey);
    }

    private static RSAParameters FromXmlString(string xmlString)
    {
        using (var stringReader = new System.IO.StringReader(xmlString))
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(RSAParameters));
            return (RSAParameters)serializer.Deserialize(stringReader);
        }
    }
}