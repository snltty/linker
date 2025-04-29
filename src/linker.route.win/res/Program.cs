using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;

namespace LinkerRoute
{
    public class LinkerRoute
    {
        public string GetWsIP(string wsServer)
        {
            return new Uri(wsServer).Host;
        }

        public string ToNetwork(string ip, byte prefixLength)
        {
            byte[] sourceBytes = IPAddress.Parse(ip).GetAddressBytes();
            Array.Reverse(sourceBytes);

            uint sourceIPInt = BitConverter.ToUInt32(sourceBytes, 0);

            uint prefixIP = GetPrefixIP(prefixLength);

            sourceBytes = new IPAddress((sourceIPInt & prefixIP)).GetAddressBytes();
            Array.Reverse(sourceBytes);
            return string.Join(".",sourceBytes);
        }
         public string ToPrefixIP(byte prefixLength)
        {
            uint prefixIP = GetPrefixIP(prefixLength);
            byte[] sourceBytes = new IPAddress(prefixIP).GetAddressBytes();
            Array.Reverse(sourceBytes);
            return string.Join(".",sourceBytes);
        }
        private uint GetPrefixIP(byte prefixLength)
        {
            //最多<<31 所以0需要单独计算
            if (prefixLength < 1) return 0;
            return 0xffffffff << (32 - prefixLength);
        }
    }
}
