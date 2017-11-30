using OpenSSL.Core;
using System;
using System.IO;
using System.Linq;

namespace RDPCertInstaller
{
    class PfxMaker
    {
        private readonly Action<string> _log;
        public PfxMaker(Action<string> log)
        {
            _log = log;
        }
        public void MakePfx(string keyPath, string certPath, string outputPath, string password="")
        { 
            string k = File.ReadAllText(keyPath);
            string c = File.ReadAllText(certPath);

            BIO certBio = new BIO(c);

            _log("Creating pfx from key and certificate ...");

            var key = OpenSSL.Crypto.CryptoKey.FromPrivateKey(k, password);
            var cert = new OpenSSL.X509.X509Chain(certBio);
            Stack<OpenSSL.X509.X509Certificate> hmm = new Stack<OpenSSL.X509.X509Certificate>();
            if (cert.Count != 0)
            {
                //all certificates except first is chain
                foreach (var certInChain in cert.Skip(1))
                    hmm.Add(certInChain);

                //private key is only for first cert
                var pfx = new OpenSSL.X509.PKCS12(null, key, cert[0], hmm);

                WriteToFile(outputPath, pfx);

                _log("Success");
            }
            else
                throw new Exception("Certificate file contains no certificates");
        }

        private static void WriteToFile(string outputPath, OpenSSL.X509.PKCS12 pfx)
        {
            using (FileStream fs = new FileStream(outputPath, FileMode.Create, FileAccess.ReadWrite))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    using (BIO bio = BIO.MemoryBuffer())
                    {
                        pfx.Write(bio);
                        while (bio.BytesPending > 0)
                        {
                            var bytes = bio.ReadBytes(50000);
                            if (bytes.Count > 0)
                                bw.Write(bytes.Array, 0, bytes.Count);
                            else
                                break;
                        }
                    }
                }
            }
        }
    }
}
