using OpenSSL.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RDPCertInstaller
{
    class PfxMaker
    {
        Action<string> Log;
        public PfxMaker(Action<string> log)
        {
            Log = log;
        }
        public void MakePfx(string keyPath, string certPath, string outputPath, string password="")
        { 
            string k = File.ReadAllText(keyPath);
            string c = File.ReadAllText(certPath);

            OpenSSL.Core.BIO key_bio = new OpenSSL.Core.BIO(k);
            OpenSSL.Core.BIO cert_bio = new OpenSSL.Core.BIO(c);

            Log("Creating pfx from key and certificate ...");

            var key = OpenSSL.Crypto.CryptoKey.FromPrivateKey(k, password);
            var cert = new OpenSSL.X509.X509Chain(cert_bio);
            OpenSSL.Core.Stack<OpenSSL.X509.X509Certificate> hmm = new OpenSSL.Core.Stack<OpenSSL.X509.X509Certificate>();
            if (cert.Count != 0)
            {
                foreach (var certInChain in cert.Skip(1))
                    hmm.Add(cert[1]);

                var pfx = new OpenSSL.X509.PKCS12(null, key, cert[0], hmm);

                WriteToFile(outputPath, pfx);

                Log("Success");
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
