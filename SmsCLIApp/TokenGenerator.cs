using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;

namespace Nexmo
{
    public class TokenGenerator
    {             
        private const string PATH_TO_PRIVATE_KEY = @".\privateKey.pem";
        private const string PATH_TO_PUBLIC_KEY = @".\publicKey.pem";

        public static string GenerateToken(List<Claim> claims, string privateKey)
        {   
            RSAParameters rsaParams;
            //var privateKey = File.ReadAllText(PATH_TO_PRIVATE_KEY);
            using (var tr = new StringReader(privateKey))
            {
                var pemReader = new PemReader(tr);
                var kp = pemReader.ReadObject();                
                var privateRsaParams = kp as RsaPrivateCrtKeyParameters;
                rsaParams = DotNetUtilities.ToRSAParameters(privateRsaParams);
            }
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(rsaParams);
                Dictionary<string, object> payload = claims.ToDictionary(k => k.Type, v => (object)v.Value);
                return Jose.JWT.Encode(payload, rsa, Jose.JwsAlgorithm.RS256);
            }            
        }

        public static string DecodeToken(string token)
        {
            RSAParameters rsaParams;
            var publicKey = File.ReadAllText(PATH_TO_PUBLIC_KEY);
            using (var tr = new StringReader(publicKey))
            {
                var pemReader = new PemReader(tr);
                var publicKeyParams = pemReader.ReadObject() as RsaKeyParameters;                
                if (publicKeyParams == null)
                {
                    throw new Exception("Could not read RSA public key");
                }
                rsaParams = DotNetUtilities.ToRSAParameters(publicKeyParams);
            }
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(rsaParams);
                // This will throw if the signature is invalid
                return Jose.JWT.Decode(token, rsa, Jose.JwsAlgorithm.RS256);
            }
        }
    }
}
