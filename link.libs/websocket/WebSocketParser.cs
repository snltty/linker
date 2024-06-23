using link.libs;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace link.libs.websocket
{
    /// <summary>
    /// websocket解析器
    /// </summary>
    public static class WebSocketParser
    {
        private readonly static SHA1 sha1 = SHA1.Create();
        private readonly static Memory<byte> magicCode = Encoding.ASCII.GetBytes("258EAFA5-E914-47DA-95CA-C5AB0DC85B11");
        /// <summary>
        /// 构建连接数据
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        public static byte[] BuildConnectData(WebsocketHeaderInfo header)
        {
            string path = header.Path.Length == 0 ? "/" : Encoding.UTF8.GetString(header.Path.Span);

            StringBuilder sb = new StringBuilder(10);
            sb.Append($"GET {path} HTTP/1.1\r\n");
            sb.Append($"Upgrade: websocket\r\n");
            sb.Append($"Connection: Upgrade\r\n");
            sb.Append($"Sec-WebSocket-Version: 13\r\n");
            sb.Append($"Sec-WebSocket-Key: {Encoding.UTF8.GetString(header.SecWebSocketKey.Span)}\r\n");
            if (header.SecWebSocketProtocol.Length > 0)
            {
                sb.Append($"Sec-WebSocket-Protocol: {Encoding.UTF8.GetString(header.SecWebSocketProtocol.Span)}\r\n");
            }
            if (header.SecWebSocketExtensions.Length > 0)
            {
                sb.Append($"Sec-WebSocket-Extensions: {Encoding.UTF8.GetString(header.SecWebSocketExtensions.Span)}\r\n");
            }
            sb.Append("\r\n");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
        /// <summary>
        /// 构建连接回应数据
        /// </summary>
        /// <param name="header"></param>
        /// <returns></returns>
        public static byte[] BuildConnectResponseData(WebsocketHeaderInfo header)
        {
            string acceptStr = BuildSecWebSocketAccept(header.SecWebSocketKey);

            StringBuilder sb = new StringBuilder(10);
            sb.Append($"HTTP/1.1 {(int)header.StatusCode} {AddSpace(header.StatusCode)}\r\n");
            sb.Append($"Sec-WebSocket-Accept: {acceptStr}\r\n");
            if (header.Connection.Length > 0)
            {
                sb.Append($"Connection: {Encoding.UTF8.GetString(header.Connection.Span)}\r\n");
            }
            if (header.Upgrade.Length > 0)
            {
                sb.Append($"Upgrade: {Encoding.UTF8.GetString(header.Upgrade.Span)}\r\n");
            }
            if (header.SecWebSocketVersion.Length > 0)
            {
                sb.Append($"Sec-WebSocket-Version: {Encoding.UTF8.GetString(header.SecWebSocketVersion.Span)}\r\n");
            }
            if (header.SecWebSocketProtocol.Length > 0)
            {
                sb.Append($"Sec-WebSocket-Protocol: {Encoding.UTF8.GetString(header.SecWebSocketProtocol.Span)}\r\n");
            }
            if (header.SecWebSocketExtensions.Length > 0)
            {
                sb.Append($"Sec-WebSocket-Extensions: {Encoding.UTF8.GetString(header.SecWebSocketExtensions.Span)}\r\n");
            }
            sb.Append("\r\n");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }
        /// <summary>
        /// 生成随机key
        /// </summary>
        /// <returns></returns>
        public static byte[] BuildSecWebSocketKey()
        {
            byte[] bytes = new byte[16];
            Random random = new Random(DateTime.Now.Ticks.GetHashCode());
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)random.Next(0, 255);
            }
            byte[] res = Encoding.UTF8.GetBytes(Convert.ToBase64String(bytes));
            return res;
        }
        /// <summary>
        /// 构建mask数据
        /// </summary>
        /// <returns></returns>
        public static byte[] BuildMaskKey()
        {
            byte[] bytes = new byte[4];

            Random random = new Random(DateTime.Now.Ticks.GetHashCode());
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)random.Next(0, 255);
            }

            return bytes;
        }
        /// <summary>
        /// 生成accept回应
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string BuildSecWebSocketAccept(Memory<byte> key)
        {
            int keyLength = key.Length + magicCode.Length;
            byte[] acceptBytes = new byte[keyLength];

            key.CopyTo(acceptBytes);
            magicCode.CopyTo(acceptBytes.AsMemory(key.Length));

            string acceptStr = Convert.ToBase64String(sha1.ComputeHash(acceptBytes, 0, keyLength));

            return acceptStr;
        }
        /// <summary>
        /// 验证回应的accept
        /// </summary>
        /// <param name="key"></param>
        /// <param name="accept"></param>
        /// <returns></returns>
        public static bool VerifySecWebSocketAccept(Memory<byte> key, Memory<byte> accept)
        {
            string acceptStr = BuildSecWebSocketAccept(key);
            return acceptStr == Encoding.UTF8.GetString(accept.Span);
        }

        /// <summary>
        /// 构建ping帧
        /// </summary>
        /// <returns></returns>
        public static byte[] BuildPingData()
        {
            return new byte[]
            {
                (byte)WebSocketFrameInfo.EnumFin.Fin | (byte)WebSocketFrameInfo.EnumOpcode.Ping, //fin + ping
                (byte)WebSocketFrameInfo.EnumMask.None | 0, //没有 mask 和 payload length
            };
        }
        /// <summary>
        /// 构建pong帧
        /// </summary>
        /// <returns></returns>
        public static byte[] BuildPongData()
        {
            return new byte[]
            {
                (byte)WebSocketFrameInfo.EnumFin.Fin | (byte)WebSocketFrameInfo.EnumOpcode.Pong, //fin + pong
                (byte)WebSocketFrameInfo.EnumMask.None | 0, //没有 mask 和 payload length
            };
        }
        /// <summary>
        /// 构建数据帧
        /// </summary>
        /// <param name="remark"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static byte[] BuildFrameData(WebSocketFrameRemarkInfo remark, out int length)
        {
            if (remark.Mask > 0 && remark.MaskData.Length != 4)
            {
                throw new Exception("mask data just 4byte");
            }

            length = 1 + 1 + remark.Data.Length;
            int index = 2;
            if (remark.Mask == WebSocketFrameInfo.EnumMask.Mask)
            {
                length += 4;
            }

            byte payloadLength;
            int dataLength = remark.Data.Length;
            if (dataLength > ushort.MaxValue)
            {
                length += 8;
                payloadLength = 127;
            }
            else if (dataLength > 125)
            {
                length += 2;
                payloadLength = 126;
            }
            else
            {
                payloadLength = (byte)dataLength;
            }


            byte[] bytes = ArrayPool<byte>.Shared.Rent(length);
            var memory = bytes.AsMemory();
            bytes[0] = (byte)((byte)remark.Fin | (byte)remark.Rsv1 | (byte)remark.Rsv2 | (byte)remark.Rsv3 | (byte)remark.Opcode);
            bytes[1] = (byte)((byte)remark.Mask | payloadLength);


            if (dataLength > ushort.MaxValue)
            {
                BinaryPrimitives.WriteUInt64BigEndian(memory.Slice(index).Span, (ulong)dataLength);
                index += 8;
            }
            else if (dataLength > 125)
            {
                BinaryPrimitives.WriteUInt16BigEndian(memory.Slice(index).Span, (ushort)dataLength);
                index += 2;
            }

            if (remark.Mask == WebSocketFrameInfo.EnumMask.Mask)
            {
                remark.MaskData.CopyTo(bytes.AsMemory(index, remark.MaskData.Length));
                index += remark.MaskData.Length;
            }

            if (remark.Data.Length > 0)
            {
                remark.Data.CopyTo(bytes.AsMemory(index));
            }

            return bytes;
        }
        public static void Return(byte[] bytes)
        {
            ArrayPool<byte>.Shared.Return(bytes);
        }

        /// <summary>
        /// 给每个大写字母前加一个空格，例如ProxyAuthenticationRequired 变成Proxy Authentication Required
        /// </summary>
        /// <param name="statusCode"></param>
        /// <returns></returns>
        private static string AddSpace(HttpStatusCode statusCode)
        {
            ReadOnlySpan<char> span = statusCode.ToString().AsSpan();

            int totalLength = span.Length * 2;

            char[] result = new char[totalLength];
            Span<char> resultSpan = result.AsSpan(0, totalLength);
            span.CopyTo(resultSpan);

            int length = 0;
            for (int i = 0; i < span.Length; i++)
            {
                if (i > 0 && span[i] >= 65 && span[i] <= 90)
                {
                    resultSpan.Slice(i + length, totalLength - (length + i) - 1).CopyTo(resultSpan.Slice(i + length + 1));
                    resultSpan[i + length] = (char)32;
                    length++;
                }
            }

            string resultStr = resultSpan.Slice(0, span.Length + length).ToString();

            return resultStr;
        }
    }

    public sealed class WebSocketFrameRemarkInfo
    {
        /// <summary>
        /// 是否是结束帧，如果只有一帧，那必定是结束帧
        /// </summary>
        public WebSocketFrameInfo.EnumFin Fin { get; set; } = WebSocketFrameInfo.EnumFin.Fin;
        /// <summary>
        /// 保留位1
        /// </summary>
        public WebSocketFrameInfo.EnumRsv1 Rsv1 { get; set; } = WebSocketFrameInfo.EnumRsv1.None;
        /// <summary>
        /// 保留位2
        /// </summary>
        public WebSocketFrameInfo.EnumRsv2 Rsv2 { get; set; } = WebSocketFrameInfo.EnumRsv2.None;
        /// <summary>
        /// 保留位3
        /// </summary>
        public WebSocketFrameInfo.EnumRsv3 Rsv3 { get; set; } = WebSocketFrameInfo.EnumRsv3.None;
        /// <summary>
        /// 数据描述，默认TEXT数据
        /// </summary>
        public WebSocketFrameInfo.EnumOpcode Opcode { get; set; } = WebSocketFrameInfo.EnumOpcode.Text;

        /// <summary>
        /// 是否有掩码
        /// </summary>
        public WebSocketFrameInfo.EnumMask Mask { get; set; } = WebSocketFrameInfo.EnumMask.None;
        /// <summary>
        /// 掩码key 4字节
        /// </summary>
        public Memory<byte> MaskData { get; set; }

        /// <summary>
        /// payload data
        /// </summary>
        public Memory<byte> Data { get; set; }
    }

    /// <summary>
    /// 数据帧解析
    /// </summary>
    public sealed class WebSocketFrameInfo
    {
        /// <summary>
        /// 是否是结束帧
        /// </summary>
        public EnumFin Fin { get; set; }
        /// <summary>
        /// 保留位
        /// </summary>
        public EnumRsv1 Rsv1 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public EnumRsv2 Rsv2 { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public EnumRsv3 Rsv3 { get; set; }

        /// <summary>
        /// 操作码 0附加数据，1文本，2二进制，3-7为非控制保留，8关闭，9ping，a pong，b-f 为控制保留
        /// </summary>
        public EnumOpcode Opcode { get; set; }
        /// <summary>
        /// 掩码
        /// </summary>
        public EnumMask Mask { get; set; }
        /// <summary>
        /// 总长度
        /// </summary>
        public int TotalLength { get; set; }
        /// <summary>
        /// 数据 如果OPCODE是 EnumOpcode.Close 则数据的前2字节为关闭状态码，余下的为其它描述数据
        /// </summary>
        public Memory<byte> PayloadData { get; set; }

        /// <summary>
        /// 解析帧，如果false解析失败，则应该把data缓存起来，等待下次来数据后，拼接起来再次解析
        /// </summary>
        /// <param name="data"></param>
        /// <param name="frameInfo"></param>
        /// <returns></returns>
        public static bool TryParse(Memory<byte> data, out WebSocketFrameInfo frameInfo)
        {
            frameInfo = null;

            //小于2字节不可解析
            if (data.Length < 2)
            {
                return false;
            }

            Span<byte> span = data.Span;
            int index = 2;

            //第2字节
            //1位 是否mask
            EnumMask mask = (EnumMask)(span[1] & (byte)EnumMask.Mask);
            int payloadLength = span[1] & 0b01111111;
            if (payloadLength == 126)
            {
                payloadLength = BinaryPrimitives.ReadUInt16BigEndian(span.Slice(2, 2));
                index += 2;
            }
            else if (payloadLength == 127)
            {
                payloadLength = (int)BinaryPrimitives.ReadUInt64BigEndian(span.Slice(2, 8));
                index += 8;
            }
            //数据长+头长 大于 整个数据长，则不是一个完整的包
            if (data.Length < payloadLength + index + (mask == EnumMask.Mask ? 4 : 0))
            {
                return false;
            }

            //第1字节
            //1位 是否是结束帧
            EnumFin fin = (EnumFin)(byte)(span[0] & (byte)EnumFin.Fin);
            //2 3 4 保留
            EnumRsv1 rsv1 = (EnumRsv1)(byte)(span[0] & (byte)EnumRsv1.Rsv);
            EnumRsv2 rsv2 = (EnumRsv2)(byte)(span[0] & (byte)EnumRsv2.Rsv);
            EnumRsv3 rsv3 = (EnumRsv3)(byte)(span[0] & (byte)EnumRsv3.Rsv);
            //5 6 7 8 操作码
            EnumOpcode opcode = (EnumOpcode)(byte)(span[0] & (byte)EnumOpcode.Last);

            Span<byte> maskKey = Helper.EmptyArray;
            if (mask == EnumMask.Mask)
            {
                //mask掩码key 用来解码数据
                maskKey = span.Slice(index, 4);
                index += 4;
            }

            //数据
            Memory<byte> payloadData = data.Slice(index, payloadLength);
            if (mask == EnumMask.Mask)
            {
                //解码
                Span<byte> payloadDataSpan = payloadData.Span;
                for (int i = 0; i < payloadDataSpan.Length; i++)
                {
                    payloadDataSpan[i] = (byte)(payloadDataSpan[i] ^ maskKey[3 & i]);
                }
            }

            frameInfo = new WebSocketFrameInfo
            {
                Fin = fin,
                Rsv1 = rsv1,
                Rsv2 = rsv2,
                Rsv3 = rsv3,
                Opcode = opcode,
                Mask = mask,
                PayloadData = payloadData,
                TotalLength = index + payloadLength
            };
            return true;
        }
        public enum EnumFin : byte
        {
            None = 0x0,
            Fin = 0b10000000,
        }
        public enum EnumMask : byte
        {
            None = 0x0,
            Mask = 0b10000000,
        }
        public enum EnumRsv1 : byte
        {
            None = 0x0,
            Rsv = 0b01000000,
        }
        public enum EnumRsv2 : byte
        {
            None = 0x0,
            Rsv = 0b00100000,
        }
        public enum EnumRsv3 : byte
        {
            None = 0x0,
            Rsv = 0b00010000,
        }
        public enum EnumOpcode : byte
        {
            Data = 0x0,
            Text = 0x1,
            Binary = 0x2,
            UnControll3 = 0x3,
            UnControll4 = 0x4,
            UnControll5 = 0x5,
            UnControll6 = 0x6,
            UnControll7 = 0x7,
            Close = 0x8,
            Ping = 0x9,
            Pong = 0xa,
            Controll11 = 0xb,
            Controll12 = 0xc,
            Controll13 = 0xd,
            Controll14 = 0xe,
            Controll15 = 0xf,
            Last = 0xf,
        }

        /// <summary>
        /// 关闭的状态码
        /// </summary>
        public enum EnumCloseStatus : ushort
        {
            /// <summary>
            /// 正常关闭
            /// </summary>
            Normal = 1000,
            /// <summary>
            /// 正在离开
            /// </summary>
            Leaving = 1001,
            /// <summary>
            /// 协议错误
            /// </summary>
            ProtocolError = 1002,
            /// <summary>
            /// 只能接收TEXT数据
            /// </summary>
            TextOnly = 1003,
            /// <summary>
            /// 保留
            /// </summary>
            None1004 = 1004,
            /// <summary>
            /// 保留
            /// </summary>
            None1005 = 1005,
            /// <summary>
            /// 保留
            /// </summary>
            None1006 = 1006,
            /// <summary>
            /// 消息类型不一致
            /// </summary>
            DataTypeError = 1007,
            /// <summary>
            /// 通用状态码，没有别的合适的状态码时，用这个
            /// </summary>
            PublicError = 1008,
            /// <summary>
            /// 数据太大，无法处理
            /// </summary>
            DataTooBig = 1009,
            /// <summary>
            /// 扩展错误
            /// </summary>
            ExtendsError = 1010,//正常关闭
            /// <summary>
            /// 意外情况
            /// </summary>
            Unexpected = 1011,
            /// <summary>
            /// TLS握手失败
            /// </summary>
            TLSError = 1015
        }
    }

    /// <summary>
    /// 请求头解析
    /// </summary>
    public sealed class WebsocketHeaderInfo
    {
        static byte[][] bytes = new byte[][] {
            Encoding.ASCII.GetBytes("Connection: "),
            Encoding.ASCII.GetBytes("Upgrade: "),
            Encoding.ASCII.GetBytes("Origin: "),
            Encoding.ASCII.GetBytes("Sec-WebSocket-Version: "),
            Encoding.ASCII.GetBytes("Sec-WebSocket-Key: "),
            Encoding.ASCII.GetBytes("Sec-WebSocket-Extensions: "),
            Encoding.ASCII.GetBytes("Sec-WebSocket-Protocol: "),
            Encoding.ASCII.GetBytes("Sec-WebSocket-Accept: ")
        };
        static byte[] httpBytes = Encoding.UTF8.GetBytes("HTTP/");

        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.SwitchingProtocols;
        public Memory<byte> Method { get; private set; }

        private string _pathSet { get; set; }
        /// <summary>
        /// 用这个设置path值
        /// </summary>
        public string PathSet
        {
            get
            {
                return _pathSet;
            }
            set
            {
                _pathSet = value;
                Path = Encoding.UTF8.GetBytes(_pathSet);
            }
        }
        /// <summary>
        /// 如果 仅1个字符，那就是 /
        /// </summary>
        public Memory<byte> Path { get; private set; }
        public Memory<byte> Connection { get; private set; }
        public Memory<byte> Upgrade { get; private set; }
        public Memory<byte> Origin { get; private set; }
        public Memory<byte> SecWebSocketVersion { get; private set; }
        public Memory<byte> SecWebSocketKey { get; set; }
        public Memory<byte> SecWebSocketExtensions { get; set; }
        public Memory<byte> SecWebSocketProtocol { get; set; }
        public Memory<byte> SecWebSocketAccept { get; set; }
        public static WebsocketHeaderInfo Parse(Memory<byte> header)
        {
            Span<byte> span = header.Span;
            int flag = 0xff;
            int bit = 0x01;

            ulong[] res = new ulong[bytes.Length];

            for (int i = 0, len = span.Length; i < len; i++)
            {
                if (span[i] == 13 && span[i + 1] == 10 && span[i + 2] == 13 && span[i + 3] == 10)
                {
                    break;
                }
                if (span[i] == 13 && span[i + 1] == 10)
                {
                    int startIndex = i + 2;
                    for (int k = 0; k < bytes.Length; k++)
                    {
                        if ((flag >> k & 1) == 1 && span[startIndex] == bytes[k][0])
                        {
                            if (span.Slice(startIndex, bytes[k].Length).SequenceEqual(bytes[k]))
                            {
                                int index = span.Slice(startIndex).IndexOf((byte)13);
                                flag &= ~(bit << k);

#pragma warning disable CS0675 // 对进行了带符号扩展的操作数使用了按位或运算符
                                res[k] = (ulong)(startIndex + bytes[k].Length) << 32 | (ulong)(index - bytes[k].Length);
#pragma warning restore CS0675 // 对进行了带符号扩展的操作数使用了按位或运算符

                                i += index + 1;
                                break;
                            }
                        }
                    }
                }
            }

            WebsocketHeaderInfo headerInfo = new WebsocketHeaderInfo
            {
                Connection = header.Slice((int)(res[0] >> 32), (int)(res[0] & 0xffffffff)),
                Upgrade = header.Slice((int)(res[1] >> 32), (int)(res[1] & 0xffffffff)),
                Origin = header.Slice((int)(res[2] >> 32), (int)(res[2] & 0xffffffff)),
                SecWebSocketVersion = header.Slice((int)(res[3] >> 32), (int)(res[3] & 0xffffffff)),
                SecWebSocketKey = header.Slice((int)(res[4] >> 32), (int)(res[4] & 0xffffffff)),
                SecWebSocketExtensions = header.Slice((int)(res[5] >> 32), (int)(res[5] & 0xffffffff)),
                SecWebSocketProtocol = header.Slice((int)(res[6] >> 32), (int)(res[6] & 0xffffffff)),
                SecWebSocketAccept = header.Slice((int)(res[7] >> 32), (int)(res[7] & 0xffffffff)),
            };

            int pathIndex = span.IndexOf((byte)32);
            int pathIndex1 = span.Slice(pathIndex + 1).IndexOf((byte)32);
            if (header.Slice(0, httpBytes.Length).Span.SequenceEqual(httpBytes))
            {
                int code = int.Parse(Encoding.UTF8.GetString(header.Slice(pathIndex + 1, pathIndex1).Span));
                headerInfo.StatusCode = (HttpStatusCode)code;
            }
            else
            {
                headerInfo.Path = header.Slice(pathIndex + 1, pathIndex1);
                headerInfo.Method = header.Slice(0, pathIndex);
            }

            return headerInfo;
        }
    }
}
