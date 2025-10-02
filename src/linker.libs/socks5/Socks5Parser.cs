using linker.libs.extends;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Text;

namespace linker.libs.socks5
{
    /// <summary>
    /// socks5 数据包解析和组装
    /// </summary>
    public sealed class Socks5Parser
    {

        /// <summary>
        /// 获取客户端过来的支持的认证方式列表
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static Socks5EnumAuthType[] GetAuthMethods(ReadOnlySpan<byte> span)
        {
            //VER       NMETHODS    METHODS
            // 1            1       1-255
            //版本     支持哪些认证     一个认证方式一个字节
            byte length = span[1];
            Socks5EnumAuthType[] res = new Socks5EnumAuthType[length];
            for (byte i = 0; i < length; i++)
            {
                res[i] = (Socks5EnumAuthType)span[2 + i];
            }
            return res;
        }
        /// <summary>
        /// 获取账号密码
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static (string username, string password) GetPasswordAuthInfo(Span<byte> span)
        {
            /*
             子版本 username长度 username password长度 password
             0x01   
             */
            string username = span.Slice(2, span[1]).GetString();
            string password = span.Slice(2 + span[1] + 1, span[2 + span[1]]).GetString();
            return (username, password);
        }

        /// <summary>
        /// 获取地址
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ReadOnlyMemory<byte> GetRemoteEndPoint(ReadOnlyMemory<byte> data, out Socks5EnumAddressType addressType, out ushort port, out int index)
        {
            //VERSION COMMAND RSV ATYPE  DST.ADDR  DST.PORT
            //去掉 VERSION COMMAND RSV
            ReadOnlyMemory<byte> memory = data.Slice(3);
            ReadOnlySpan<byte> span = memory.Span;
            addressType = (Socks5EnumAddressType)span[0];
            index = 0;
            ReadOnlyMemory<byte> result = Helper.EmptyArray;

            switch (addressType)
            {
                case Socks5EnumAddressType.IPV4:
                    {
                        result = memory.Slice(1, 4);
                        index = 1 + 4;
                    }
                    break;
                case Socks5EnumAddressType.Domain:
                    {
                        result = memory.Slice(2, span[1]);
                        index = 2 + span[1];
                    }
                    break;
                case Socks5EnumAddressType.IPV6:
                    {
                        result = memory.Slice(1, 16);
                        index = 1 + 16;
                    }
                    break;
                default:
                    break;
            }

            port = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(index, 2));
            index += 2;
            index += 3;
            return result;
        }

        /// <summary>
        /// 获取udp中继中的数据
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static ReadOnlyMemory<byte> GetUdpData(ReadOnlyMemory<byte> span)
        {
            //RSV FRAG ATYPE DST.ADDR DST.PORT DATA
            //去掉 RSV FRAG   RSV占俩字节
            span = span.Slice(3);
            return (Socks5EnumAddressType)span.Span[0] switch
            {
                Socks5EnumAddressType.IPV4 => span[(1 + 4 + 2)..],
                Socks5EnumAddressType.IPV6 => span[(1 + 16 + 2)..],
                Socks5EnumAddressType.Domain => span[(2 + span.Span[1] + 2)..],
                _ => Helper.EmptyArray,
            };
        }

        /// <summary>
        /// 生成connect返回包
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <param name="responseCommand"></param>
        /// <returns></returns>
        public static unsafe Memory<byte> MakeConnectResponse(Memory<byte> memory,IPEndPoint remoteEndPoint, byte responseCommand)
        {
            //VER REP  RSV ATYPE BND.ADDR BND.PORT

            memory.Span[0] = 5;
            memory.Span[1] = responseCommand;
            memory.Span[2] = 0;
            memory.Span[3] = (byte)(remoteEndPoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? Socks5EnumAddressType.IPV4 : Socks5EnumAddressType.IPV6);

            remoteEndPoint.Address.TryWriteBytes(memory.Span.Slice(4), out _);

            int port = remoteEndPoint.Port;
            ref int _port = ref port;
            fixed (void* p = &_port)
            {
                byte* pp = (byte*)p;
                memory.Span[^2] = *(pp + 1);
                memory.Span[^1] = *pp;
            }

            return memory.Slice(0, 6 + (remoteEndPoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 4 : 16));
        }
        /// <summary>
        /// 生成udp中中继数据包
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <param name="data"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static unsafe int MakeUdpResponse(IPEndPoint remoteEndPoint, ReadOnlyMemory<byte> data,Memory<byte> target)
        {
            //RSV FRAG ATYPE DST.ADDR DST.PORT DATA
            //RSV占俩字节

            int ipLength = (remoteEndPoint.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 4 : 16);
            int length = 4 + ipLength + 2 + data.Length;

            var span = target.Span;

            span[0] = 0;
            span[1] = 0;
            span[2] = 0; //FRAG
            span[3] = (byte)(ipLength == 4 ? Socks5EnumAddressType.IPV4 : Socks5EnumAddressType.IPV6);

            int index = 4;

            remoteEndPoint.Address.TryWriteBytes(span.Slice(index), out _);
            index += ipLength;

            int port = remoteEndPoint.Port;
            ref int _port = ref port;
            fixed (void* p = &_port)
            {
                byte* pp = (byte*)p;
                span[index] = *(pp + 1);
                span[index + 1] = *pp;
            }
            index += 2;

            return length;
        }

        public static EnumProxyValidateDataResult ValidateData(Socks5EnumStep step, Memory<byte> data)
        {
            return step switch
            {
                Socks5EnumStep.Request => ValidateRequestData(data),
                Socks5EnumStep.Command => ValidateCommandData(data),
                Socks5EnumStep.Auth => ValidateAuthData(data, Socks5EnumAuthType.Password),
                Socks5EnumStep.Forward => EnumProxyValidateDataResult.Equal,
                Socks5EnumStep.ForwardUdp => EnumProxyValidateDataResult.Equal,
                _ => EnumProxyValidateDataResult.Equal
            };
        }
        /// <summary>
        /// 验证 request数据完整性
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static EnumProxyValidateDataResult ValidateRequestData(ReadOnlyMemory<byte> data)
        {
            /*
             * VERSION	METHODS_COUNT	METHODS
             * 1字节	1字节	        1到255字节，长度由METHODS_COUNT值决定
             *  0x05	0x03	        0x00 0x01 0x02
             */
            if (data.Length < 2 || data.Length < 2 + data.Span[1])
            {
                return EnumProxyValidateDataResult.TooShort;
            }
            return EnumProxyValidateDataResult.Equal;
        }
        /// <summary>
        /// 验证command数据完整性
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static EnumProxyValidateDataResult ValidateCommandData(ReadOnlyMemory<byte> data)
        {
            /*
             * VERSION  COMMAND RSV ADDRESS_TYPE    DST.ADDR    DST.PORT
             * 1        1       1   1               1-255       2
             * 域名模式下 DST.ADDR第一个字节是域名长度，那么整个数据至少8个字节
             */
            if (data.Length < 8) return EnumProxyValidateDataResult.TooShort;

            var span = data.Span;
            int addrLength = (Socks5EnumAddressType)span[3] switch
            {
                Socks5EnumAddressType.IPV4 => 4 + 2,
                Socks5EnumAddressType.Domain => span[4] + 1 + 2, //DST.ADDR第一个字节是域名长度 剩下的才是域名数据
                Socks5EnumAddressType.IPV6 => 16 + 2,
                _ => throw new NotImplementedException(),
            };
            if (data.Length < 4 + addrLength)
            {
                return EnumProxyValidateDataResult.TooShort;
            }
            return EnumProxyValidateDataResult.Equal;
        }
        /// <summary>
        /// 验证认证数据完整性
        /// </summary>
        /// <param name="data"></param>
        /// <param name="authType"></param>
        /// <returns></returns>
        public static EnumProxyValidateDataResult ValidateAuthData(ReadOnlyMemory<byte> data, Socks5EnumAuthType authType)
        {
            return authType switch
            {
                Socks5EnumAuthType.NoAuth => EnumProxyValidateDataResult.Equal,
                Socks5EnumAuthType.Password => ValidateAuthPasswordData(data),
                Socks5EnumAuthType.GSSAPI => EnumProxyValidateDataResult.Equal,
                Socks5EnumAuthType.IANA => EnumProxyValidateDataResult.Equal,
                Socks5EnumAuthType.UnKnow => EnumProxyValidateDataResult.Bad,
                Socks5EnumAuthType.NotSupported => EnumProxyValidateDataResult.Bad,
                _ => EnumProxyValidateDataResult.Bad,
            };
        }
        private static EnumProxyValidateDataResult ValidateAuthPasswordData(ReadOnlyMemory<byte> data)
        {
            /*
             VERSION	USERNAME_LENGTH	USERNAME	PASSWORD_LENGTH	PASSWORD
                1字节	1字节	        1到255字节	1字节	        1到255字节
                0x01	0x01	        0x0a	    0x01	        0x0a
             */

            var span = data.Slice(1).Span;
            //至少有 USERNAME_LENGTH  PASSWORD_LENGTH 字节以上
            if (span.Length <= 2)
            {
                return EnumProxyValidateDataResult.TooShort;
            }

            byte nameLength = span[0];
            //至少有 USERNAME_LENGTH USERNAME  PASSWORD_LENGTH
            if (span.Length < nameLength + 1 + 1)
            {
                return EnumProxyValidateDataResult.TooShort;
            }

            byte passwordLength = span[1 + nameLength];
            if (span.Length < 1 + 1 + nameLength + passwordLength)
            {
                return EnumProxyValidateDataResult.TooShort;
            }
            return EnumProxyValidateDataResult.Equal;
        }

    }
}
