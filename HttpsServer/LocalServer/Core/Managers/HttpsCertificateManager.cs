using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace HttpsServer.LocalServer.Core.Managers
{
    internal class HttpsCertificateManager
    {
        private static readonly Lazy<HttpsCertificateManager> _instance = new(() => new HttpsCertificateManager());

        private readonly Dictionary<IPAddress, (X509Certificate2 Certificate, string DerEncodedSerialNumber)> _certificates = [];

        private HttpsCertificateManager() { }

        public static HttpsCertificateManager Instance => _instance.Value;

        public void AddCertificate(IPAddress ipAddress, X509Certificate2 certificate)
        {
            _certificates.Add(ipAddress, (certificate, ConvertSerialNumberToDer(certificate)));
        }

        public X509Certificate2 GetCertificate(IPAddress ipAddress)
        {
            _certificates.TryGetValue(ipAddress, out var certificateInfo);
            return certificateInfo.Certificate;
        }

        public bool CompareDerEncodedSerialNumber(IPAddress ipAddress, string serialNumber)
        {
            _certificates.TryGetValue(ipAddress, out var certificateInfo);
            return certificateInfo.DerEncodedSerialNumber == serialNumber;
        }

        private string ConvertSerialNumberToDer(X509Certificate2 certificate)
        {
            string serialNumberHex = certificate.SerialNumber;
            byte[] serialNumberBytes = HexStringToByteArray(serialNumberHex);

            return Convert.ToBase64String(serialNumberBytes);
            //return Convert.ToBase64String(Convert.FromHexString(serialNumberHex));
        }

        private byte[] HexStringToByteArray(string hex)
        {
            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}
