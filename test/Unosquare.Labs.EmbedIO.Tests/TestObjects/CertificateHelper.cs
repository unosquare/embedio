namespace Unosquare.Labs.EmbedIO.Tests.TestObjects
{
    using Swan;
    using System;
    using System.IO;
    using Org.BouncyCastle.Asn1.X509;
    using Org.BouncyCastle.Crypto;
    using Org.BouncyCastle.Crypto.Generators;
    using Org.BouncyCastle.Crypto.Operators;
    using Org.BouncyCastle.Crypto.Prng;
    using Org.BouncyCastle.Math;
    using Org.BouncyCastle.Pkcs;
    using Org.BouncyCastle.Security;
    using Org.BouncyCastle.X509;

    /// <summary>
    /// Provides static methods to create, save and load certificate files.
    /// </summary>
    internal static class CertificateHelper
    {
        /// <summary>
        /// Generates an X.509 Certificate.
        /// </summary>
        /// <param name="subjectName">Name of the subject.</param>
        /// <param name="keyPair">The key pair.</param>
        /// <returns>A new X.509 Certificate.</returns>
        public static X509Certificate GenerateCertificate(string subjectName, out AsymmetricCipherKeyPair keyPair)
        {
            var keyPairGenerator = new RsaKeyPairGenerator();

            // certificate strength 2048 bits
            keyPairGenerator.Init(new KeyGenerationParameters(
                  new SecureRandom(new CryptoApiRandomGenerator()), 2048));

            keyPair = keyPairGenerator.GenerateKeyPair();

            var certGenerator = new X509V3CertificateGenerator();
            var certName = new X509Name($"CN={subjectName}");
            var serialNo = BigInteger.ProbablePrime(120, new Random());

            certGenerator.SetSerialNumber(serialNo);
            certGenerator.SetSubjectDN(certName);
            certGenerator.SetIssuerDN(certName);
            certGenerator.SetNotAfter(DateTime.Now.AddYears(100));
            certGenerator.SetNotBefore(DateTime.Now.Subtract(TimeSpan.FromHours(8)));
            certGenerator.SetPublicKey(keyPair.Public);

            certGenerator.AddExtension(
                X509Extensions.AuthorityKeyIdentifier.Id,
                false,
                new AuthorityKeyIdentifier(SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(keyPair.Public), new GeneralNames(new GeneralName(certName)), serialNo));

            /* 
             1.3.6.1.5.5.7.3.1 - id_kp_serverAuth 
             1.3.6.1.5.5.7.3.2 - id_kp_clientAuth 
             1.3.6.1.5.5.7.3.3 - id_kp_codeSigning 
             1.3.6.1.5.5.7.3.4 - id_kp_emailProtection 
             1.3.6.1.5.5.7.3.5 - id-kp-ipsecEndSystem 
             1.3.6.1.5.5.7.3.6 - id-kp-ipsecTunnel 
             1.3.6.1.5.5.7.3.7 - id-kp-ipsecUser 
             1.3.6.1.5.5.7.3.8 - id_kp_timeStamping 
             1.3.6.1.5.5.7.3.9 - OCSPSigning
             */
            certGenerator.AddExtension(
                X509Extensions.ExtendedKeyUsage.Id,
                false,
                new ExtendedKeyUsage(KeyPurposeID.IdKPServerAuth));

            var signatureFactory = new Asn1SignatureFactory("SHA256withRSA", keyPair.Private);
            var generatedCertificate = certGenerator.Generate(signatureFactory);

            return generatedCertificate;
        }

        /// <summary>
        /// Saves the given X.509 certificate to a file.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        /// <param name="keyPair">The key pair.</param>
        /// <param name="outputFilePath">The output file path.</param>
        /// <param name="certificateAlias">The certificate alias.</param>
        /// <param name="certificatePassword">The certificate password.</param>
        public static void SaveToFile(this X509Certificate certificate, AsymmetricCipherKeyPair keyPair, string outputFilePath, string certificateAlias, string certificatePassword)
        {
            var certificateStore = new Pkcs12Store();
            var certificateEntry = new X509CertificateEntry(certificate);

            certificateStore.SetCertificateEntry(certificateAlias, certificateEntry);
            certificateStore.SetKeyEntry(certificateAlias, new AsymmetricKeyEntry(keyPair.Private), new[] { certificateEntry });

            using (var outputFileStream = File.Create(outputFilePath))
            {
                certificateStore.Save(
                    outputFileStream, 
                    certificatePassword == null ? new char[0] : certificatePassword.ToCharArray(), 
                    new SecureRandom(new CryptoApiRandomGenerator()));
            }
        }

        /// <summary>
        /// Creates or loads a PFX certificate.
        /// </summary>
        /// <param name="pfxFilePath">The PFX file path.</param>
        /// <param name="hostname">The hostname.</param>
        /// <param name="password">The password.</param>
        /// <returns>A valid certificate.</returns>
        public static System.Security.Cryptography.X509Certificates.X509Certificate2 CreateOrLoadCertificate(string pfxFilePath, string hostname, string password = null)
        {
            try
            {
                var certificateFilePath = Path.GetFullPath(pfxFilePath);

                if (!File.Exists(certificateFilePath))
                {
                    var certificate = GenerateCertificate(hostname, out var keyPair);
                    certificate.SaveToFile(keyPair, certificateFilePath, hostname, password);
                }

                return password == null 
                    ? new System.Security.Cryptography.X509Certificates.X509Certificate2(certificateFilePath)
                    : new System.Security.Cryptography.X509Certificates.X509Certificate2(certificateFilePath, password);
            }
            catch (Exception ex)
            {
                ex.Log(nameof(CertificateHelper));
            }

            return null;
        }
    }
}
