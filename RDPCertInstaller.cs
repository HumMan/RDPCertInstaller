using Microsoft.Win32;
using System;
using System.Linq;
using System.Security;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace RDPCertInstaller
{
    class RDPCertInstaller
    {
        private readonly Action<string> Log;
        public RDPCertInstaller(Action<string> log)
        {
            Log = log;
        }
        private void ImportPfxCollection(string certPath, string certPass = "")
        {
            // Create a collection object and populate it using the PFX file
            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.Import(certPath, certPass, X509KeyStorageFlags.MachineKeySet);

            foreach (X509Certificate2 cert in collection)
            {
                InstallCertificate(cert);
            }
        }

        public void SetupRdpWithCert(string certPath, out bool registryThumbprintChanged)
        {
            ImportPfxCollection(certPath);
            var thumbprint = GetCertThumbprint(certPath);
            UpdateRDPSSLCertHashInRegistry(thumbprint, out registryThumbprintChanged);
        }

        private void InstallCertificate(string path, SecureString password)
        {
            X509Certificate2 certificate = new X509Certificate2(path, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
            InstallCertificate(certificate);
        }

        private static string GetCertThumbprint(string certPath, SecureString password = null)
        {
            X509Certificate2 certificate = new X509Certificate2(certPath, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
            return certificate.Thumbprint;
        }

        private void InstallCertificate(X509Certificate2 certificate)
        {
            var thumbprint = certificate.Thumbprint;

            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);

            store.Open(OpenFlags.ReadWrite);
            store.Add(certificate);

            foreach (var c in store.Certificates)
            {
                if (c.Thumbprint == thumbprint)
                    ChangeCertPermissions(c);
            }
            store.Close();
        }

        private void ChangeCertPermissions(X509Certificate2 certificate)
        {
            var rsa = certificate.PrivateKey as RSACryptoServiceProvider;
            if (rsa != null)
            {
                Log("Changing cert permissions ...");
                // Modifying the CryptoKeySecurity of a new CspParameters and then instantiating
                // a new RSACryptoServiceProvider seems to be the trick to persist the access rule.
                // cf. http://blogs.msdn.com/b/cagatay/archive/2009/02/08/removing-acls-from-csp-key-containers.aspx
                var cspParams = new CspParameters(rsa.CspKeyContainerInfo.ProviderType, rsa.CspKeyContainerInfo.ProviderName, rsa.CspKeyContainerInfo.KeyContainerName)
                {
                    Flags = CspProviderFlags.UseExistingKey | CspProviderFlags.UseMachineKeyStore,
                    CryptoKeySecurity = rsa.CspKeyContainerInfo.CryptoKeySecurity,
                    KeyNumber = (int)rsa.CspKeyContainerInfo.KeyNumber
                };

                var list = cspParams.CryptoKeySecurity.GetAccessRules(true, true, typeof(SecurityIdentifier));
                bool allreadyExists = false;
                foreach (var r in list)
                {
                    var accessRule = r as CryptoKeyAccessRule;
                    if (accessRule.IdentityReference.Value == "S-1-5-20")
                        allreadyExists = true;
                }
                if (!allreadyExists)
                {
                    cspParams.CryptoKeySecurity.AddAccessRule(new CryptoKeyAccessRule(@"NT AUTHORITY\NETWORK SERVICE", CryptoKeyRights.GenericRead, AccessControlType.Allow));

                    using (var rsa2 = new RSACryptoServiceProvider(cspParams))
                    {
                        // Only created to persist the rule change in the CryptoKeySecurity
                    }

                    Log("Success");
                }
                else
                    Log("Nothing changed");
            }
        }

        //https://support.microsoft.com/ru-ru/help/2001849/how-to-force-remote-desktop-services-on-windows-7-to-use-a-custom-serv
        private void UpdateRDPSSLCertHashInRegistry(string newHash, out bool valueChanged)
        {
            Log("Updating SSLCertificateSHA1Hash in registry ...");
            using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", true))
            {
                var targetName = "SSLCertificateSHA1Hash";
                var newValue = HexStringToByteArray(newHash);
                if (key.GetValueNames().Contains(targetName)
                    && IsEqual(key.GetValue(targetName) as byte[], newValue))
                {
                    valueChanged = false;
                    Log("Nothing changed");
                }
                else
                {
                    valueChanged = true;
                    key.SetValue(targetName, newValue, RegistryValueKind.Binary);
                    Log("Success");
                }
            }

        }

        private static bool IsEqual(byte[] a, byte[] b)
        {
            var length = a.Length;
            if (length == b.Length)
            {
                for (int i = 0; i < length; i++)
                    if (a[i] != b[i])
                        return false;
                return true;
            }
            else
                return false;            
        }

        //https://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array
        public static byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 == 1)
                throw new Exception("The binary key cannot have an odd number of digits");

            byte[] arr = new byte[hex.Length >> 1];
            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }
            return arr;
        }

        public static int GetHexVal(char hex)
        {
            int val = (int)hex;
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}
