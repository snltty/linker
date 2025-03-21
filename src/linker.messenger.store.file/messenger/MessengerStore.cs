using linker.libs;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace linker.messenger.store.file.messenger
{
    public class MessengerStore : IMessengerStore
    {
        public X509Certificate Certificate => certificate;

        private readonly FileConfig fileConfig;

        private X509Certificate2 certificate;
        public MessengerStore(FileConfig fileConfig)
        {
            this.fileConfig = fileConfig;

            if (OperatingSystem.IsAndroid())
            {
                certificate = LoadCertificate(str);
            }
            else
            {
                using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"linker.messenger.store.file.{Helper.GlobalString}.pfx");
                using MemoryStream memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                certificate = new X509Certificate2(memoryStream.ToArray(), Helper.GlobalString, X509KeyStorageFlags.Exportable);
            }
            if (certificate == null)
            {
                Environment.Exit(0);
            }
        }

        private string str = @"-----BEGIN CERTIFICATE-----
MIIFGTCCBAGgAwIBAgISBl90jCZ1O8KnWcG9z3hBRB92MA0GCSqGSIb3DQEBCwUA
MDMxCzAJBgNVBAYTAlVTMRYwFAYDVQQKEw1MZXQncyBFbmNyeXB0MQwwCgYDVQQD
EwNSMTEwHhcNMjUwMzIwMDg1ODE3WhcNMjUwNjE4MDg1ODE2WjAXMRUwEwYDVQQD
DAwqLnNubHR0eS5jb20wggEiMA0GCSqGSIb3DQEBAQUAA4IBDwAwggEKAoIBAQC7
7m9vY6IsWrlxtcsoKX3ebk8FZ8i2HFQz0sO/jBOzfLZeOtXdYpAO+focWQoMiWFU
Gl6l9WP3xdwG890aQ23QqGGZwOWG+KlT8LgrIt2/lEq0OQ6apxaHav7IAyB09kNh
M1oyXtYA5fuH9sJ+unjbL4a7F5lOxUyumwSigPdOEvBzLL4GJsKQgCsyfumeAz+S
Y6ay4Ckwp0zluLwvw8IFhLmIbk7IXBwyr7ZN5ZCx7TpUN18efLDZcdQa/mTwQrCI
66ralfkXp/t6uW4FLmi7Bygxqf8XE5WQ5ES6U8Xw9pmeJlUmHCuPnvlP06Dwj5Y0
K6n8NuEZ/8kWQSoD6jxzAgMBAAGjggJBMIICPTAOBgNVHQ8BAf8EBAMCBaAwHQYD
VR0lBBYwFAYIKwYBBQUHAwEGCCsGAQUFBwMCMAwGA1UdEwEB/wQCMAAwHQYDVR0O
BBYEFCLxyWr4VY/fhtCl5YOi4PbKlH65MB8GA1UdIwQYMBaAFMXPRqTq9MPAemyV
xC2wXpIvJuO5MFcGCCsGAQUFBwEBBEswSTAiBggrBgEFBQcwAYYWaHR0cDovL3Ix
MS5vLmxlbmNyLm9yZzAjBggrBgEFBQcwAoYXaHR0cDovL3IxMS5pLmxlbmNyLm9y
Zy8wFwYDVR0RBBAwDoIMKi5zbmx0dHkuY29tMBMGA1UdIAQMMAowCAYGZ4EMAQIB
MC8GA1UdHwQoMCYwJKAioCCGHmh0dHA6Ly9yMTEuYy5sZW5jci5vcmcvMTE5LmNy
bDCCAQQGCisGAQQB1nkCBAIEgfUEgfIA8AB3ABNK3xq1mEIJeAxv70x6kaQWtyNJ
zlhXat+u2qfCq+AiAAABlbL750kAAAQDAEgwRgIhALNrxvjVBcl4MMuOY9EvxIga
R975F8z8hFyrNvnU6q+LAiEAopIUdkaOlTYd8vd/IcpDB22HNY7jtjhVh+PMfq18
sAsAdQBOdaMnXJoQwzhbbNTfP1LrHfDgjhuNacCx+mSxYpo53wAAAZWy++5kAAAE
AwBGMEQCIFbq4pwZ2OSvUyj89qqiFT2jUtA0fl6osPQVLUI3WgqqAiBNsQKOPEnO
RR/aP2AgD9j1XymGzs77uaXw3FfYFeiFpjANBgkqhkiG9w0BAQsFAAOCAQEAkU1I
5+b6t55vQkMeeGUGjImq0cdCIaoTn4ViH36nRYzq1W3d6+9fw4U8LiFuy6qnamcu
abdpzUZkyZyeqE/N5cpSWLggC0KYJupd2TgA1WKZULKBwwJcrftiqTsyb76rL1nY
SIUwK7xAoNM3Oaps9MZuTs1wwgOw0jD+Z5hOgDs9oAb6IzOY1mMb4pX2iKnx8CVI
rGCEDOOyniB6RzmmSZRWwghI/GqZZEaUxlrt/y7t5jZFVNvCcez/eJKci1+xXpP3
fZ/TnkUdWkXazGE3YNqfTwW8LvZ0VlbT62kwiKhonW9u4HWFrz0/TwreyHPpuWRH
UrFq2/AYFWWc14AHog==
-----END CERTIFICATE-----
-----BEGIN RSA PRIVATE KEY-----
MIIEpgIBAAKCAQEAu+5vb2OiLFq5cbXLKCl93m5PBWfIthxUM9LDv4wTs3y2XjrV
3WKQDvn6HFkKDIlhVBpepfVj98XcBvPdGkNt0KhhmcDlhvipU/C4KyLdv5RKtDkO
mqcWh2r+yAMgdPZDYTNaMl7WAOX7h/bCfrp42y+GuxeZTsVMrpsEooD3ThLwcyy+
BibCkIArMn7pngM/kmOmsuApMKdM5bi8L8PCBYS5iG5OyFwcMq+2TeWQse06VDdf
Hnyw2XHUGv5k8EKwiOuq2pX5F6f7erluBS5ouwcoMan/FxOVkOREulPF8PaZniZV
Jhwrj575T9Og8I+WNCup/DbhGf/JFkEqA+o8cwIDAQABAoIBAQCWVPitSS0sf/KC
RKNb4D0A1oq9zzSegPDWd84AhwQneybDIBt+Od71LABn9s30E11ZErMN3Mt4I72L
jj4bST8qP9e7T7QMERQawqAAWaa7HtvXaSEGk4yRDQT8aIvpJCnoMlT/oB9enppk
O/9/spkE9PK47PrFKUzbC8RcXf/2Ygyy8e1zsq/GwlpQoEO8/jNHE5qUDiPH7XCx
gXUY0CvYNOhq/UIWdnaRq+zjDS+UZAfVSyTkISbMruLbpxVA2DI+IWhZ/srLatZk
sg7HliTqQ8GftJCCwylMYxvdc8hinOvQ+MyOP4A2l0tG7bivwpD+KN+UYJzKWr4C
2nSq65gxAoGBAPb+rOEb9nr6Rh31JuLIDIQhVuyZ9WzeHNus8AE9UEJpoUHznAsD
CDQHC/fwTg6Va6ntHDnTOzC+buvBGsew2f8S5Kq9y0y19GMG0QxGsGa68V/3fJ1Y
aoP69f28nl6wLfKGsfRVbkmkTSGo9uVClrGZnhI9DKolDIIs2aVh21tLAoGBAMLI
feNhxtNJgvd/94nXEpReuokA7qSZhDuUmz7Zj3OOMUW+hnGEhoQR3SW7iekR8hr2
BZNptQMWZfcDPNSFRm13W3ZTu0NbvbHgdpUJ44CBq9jmqNjD9dOvGLw2rqaUttO5
ap05fcGzG2pOjhizmK5go697irhJpC68thrwoIJ5AoGBAO0wPiZiqv0H0EO+g1wu
jc5pEcdeRdAJMB9I4KXgEm3kcCYcYdI1VDBaQAUWMG9S0DQ7beqdzW6OwYgGRchU
LdQEebD5V/zPkuNxzViDnazZgygMSw/ysz8Qxh+nfVIiz8zhyox/acywSh2z23zn
yDy+74xGMh5GO8/acBugcH0nAoGBAIJjshnnlzBTUm+Z4zwLlC1sKFSUIixfc+xq
c24kN2o0cnlsrBULkPyR9MdQfmZeFALGVD81EO4rLUCokyz0touKzdgs/vt9S0Pq
rZbhUsDwjPgamEbbDxGQce90b3+lp6mdwmfTV4K2AhEs2uRgPUHmAUz0V408BsMe
oozwLZKJAoGBAKEpU57UZEG35jV42nFUAKx3e5FV5pok+lHfqbPhRi9ym6Aro+8h
nbzbVVlqAIMHcIHb8XeXqvakATt7SdJfxKSWjLA64FJP4n7hpT5T5Kq/jvIBBClz
Yr8Vb0yL3MbH7U/USqY5OLGA4E5C+MYoOYndlfQHO8r2A+ZQ9v3cs4iM
-----END RSA PRIVATE KEY-----";

        private bool VerifyCertificate(string str)
        {
            return str.StartsWith("-----BEGIN CERTIFICATE-----") && str.EndsWith("-----END RSA PRIVATE KEY-----");
        }
        private X509Certificate2 LoadCertificate(string str)
        {
            if (VerifyCertificate(str) == false) return null;

            X509Certificate2 certificate = new X509Certificate2(GetBase64Content(str, "-----BEGIN CERTIFICATE-----", "-----END CERTIFICATE-----"));

            using RSA rsa = RSA.Create();
            rsa.ImportRSAPrivateKey(GetBase64Content(str, "-----BEGIN RSA PRIVATE KEY-----", "-----END RSA PRIVATE KEY-----"), out _);
            return certificate.CopyWithPrivateKey(rsa);
        }
        private byte[] GetBase64Content(ReadOnlySpan<char> chars, ReadOnlySpan<char> startChars, ReadOnlySpan<char> endChars)
        {
            int start = chars.IndexOf(startChars) + startChars.Length;
            int end = chars.IndexOf(endChars) - start;
            chars = chars.Slice(start, end);

            str = chars.ToString();
            return Convert.FromBase64String(Regex.Replace(str, @"\r\n?|\n", ""));
        }
    }
}
