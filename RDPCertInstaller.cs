﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
        private readonly Action<string> _log;
        public RDPCertInstaller(Action<string> log)
        {
            _log = log;
        }
        private void ImportPfxCollection(string certPath, string certPass = "")
        {
            // Create a collection object and populate it using the PFX file
            X509Certificate2Collection collection = new X509Certificate2Collection();
            collection.Import(certPath, certPass, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

            InstallCertificate(collection.Cast<X509Certificate2>().ToArray());
        }

        public void SetupRdpWithCert(string certPath, out bool registryThumbprintChanged)
        {
            var thumbprint = GetCertThumbprint(certPath);
            ImportPfxCollection(certPath);
            ChangeCertPermissions(thumbprint);
            UpdateRDPSSLCertHashInRegistry(thumbprint, out registryThumbprintChanged);
        }

        private static string GetCertThumbprint(string certPath, SecureString password = null)
        {
            X509Certificate2 certificate = new X509Certificate2(certPath, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);
            return certificate.Thumbprint;
        }

        private void ChangeCertPermissions(string thumbprint)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);

            store.Open(OpenFlags.ReadOnly);

            foreach (var c in store.Certificates)
            {
                if (c.Thumbprint == thumbprint)
                {
                    ChangeCertPermissions(c);
                }
            }

            store.Close();
        }

        private void InstallCertificate(X509Certificate2[] certificates)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);

            store.Open(OpenFlags.ReadWrite);
            foreach (var certificate in certificates)
                store.Add(certificate);
            store.Close();
        }

        private void ChangeCertPermissions(X509Certificate2 certificate)
        {
            if (certificate.PrivateKey is RSACryptoServiceProvider rsa)
            {
                _log("Changing cert permissions ...");
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
                bool allreadyExists = IdentityRefExists(list, "S-1-5-20");
                if (!allreadyExists)
                {
                    cspParams.CryptoKeySecurity.AddAccessRule(new CryptoKeyAccessRule(@"NT AUTHORITY\NETWORK SERVICE", CryptoKeyRights.GenericRead, AccessControlType.Allow));

                    using (var rsa2 = new RSACryptoServiceProvider(cspParams))
                    {
                        // Only created to persist the rule change in the CryptoKeySecurity
                    }

                    _log("Success");
                }
                else
                    _log("Nothing changed");
            }
        }

        private static bool IdentityRefExists(AuthorizationRuleCollection list, string identity)
        {
            bool allreadyExists = false;
            foreach (var r in list)
            {
                if (r is CryptoKeyAccessRule accessRule)
                {
                    if (accessRule.IdentityReference.Value == identity)
                        allreadyExists = true;
                }
            }
            return allreadyExists;
        }

        //https://support.microsoft.com/ru-ru/help/2001849/how-to-force-remote-desktop-services-on-windows-7-to-use-a-custom-serv
        private void UpdateRDPSSLCertHashInRegistry(string newHash, out bool valueChanged)
        {
            _log("Updating SSLCertificateSHA1Hash in registry ...");
            using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp", true))
            {
                if (key == null)
                    throw new Exception("Registry key directory not found");
                var targetName = "SSLCertificateSHA1Hash";
                var newValue = HexStringToByteArray(newHash);
                if (key.GetValueNames().Contains(targetName)
                    && IsEqual(key.GetValue(targetName) as byte[], newValue))
                {
                    valueChanged = false;
                    _log("Nothing changed");
                }
                else
                {
                    valueChanged = true;
                    key.SetValue(targetName, newValue, RegistryValueKind.Binary);
                    _log("Success");
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
        private static byte[] HexStringToByteArray(string hex)
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

        private static int GetHexVal(char hex)
        {
            int val = hex;
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
    }
}
