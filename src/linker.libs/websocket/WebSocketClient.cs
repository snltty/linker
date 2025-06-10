using linker.libs.extends;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace linker.libs.websocket
{
    /// <summary>
    /// wensocket客户端
    /// </summary>
    public sealed class WebSocketClient : IDisposable
    {
        private int bufferSize = 4 * 1024;
        private SocketAsyncEventArgs readEventArgs;
        private bool connected ;
        private bool connecSuccess;


        /// <summary>
        /// 连接中，false表示失败，会关闭连接
        /// </summary>
        public Func<WebsocketHeaderInfo, bool> OnConnecting = (header) => { return true; };
        /// <summary>
        /// 连接失败
        /// </summary>
        public Action<string> OnConnectFail { get; set; } = (msg) => { };
        /// <summary>
        /// 已断开连接,未收到关闭帧
        /// </summary>
        public Action OnDisConnectd = () => { };

        /// <summary>
        /// 已连接
        /// </summary>
        public Action<WebsocketHeaderInfo> OnOpen = (header) => { };
        /// <summary>
        /// 已断开连接,收到关闭帧
        /// </summary>
        public Action OnClose = () => { };

        /// <summary>
        /// 文本数据
        /// </summary>
        public Action<WebSocketFrameInfo, string> OnMessage = (frame, messgae) => { };
        /// <summary>
        /// 二进制数据
        /// </summary>
        public Action<WebSocketFrameInfo, Memory<byte>> OnBinary = (frame, data) => { };

        /// <summary>
        /// 控制帧
        /// </summary>
        public Action<WebSocketFrameInfo> OnControll = (frame) => { };
        /// <summary>
        /// 非控制帧
        /// </summary>
        public Action<WebSocketFrameInfo> OnUnControll = (frame) => { };
        /// <summary>
        /// 
        /// </summary>
        public WebSocketClient()
        {
            handles = new Dictionary<WebSocketFrameInfo.EnumOpcode, Action<AsyncServerUserToken>> {
                //直接添加数据
                { WebSocketFrameInfo.EnumOpcode.Data,HandleAppendData},
                //记录opcode并添加
                { WebSocketFrameInfo.EnumOpcode.Text,HandleData},
                { WebSocketFrameInfo.EnumOpcode.Binary,HandleData},

                { WebSocketFrameInfo.EnumOpcode.Close,HandleClose},

                { WebSocketFrameInfo.EnumOpcode.Ping,HandlePing},
                { WebSocketFrameInfo.EnumOpcode.Pong,HandlePong},
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        public void Connect(IPAddress ip, int port)
        {
            Connect(new IPEndPoint(ip, port));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ep"></param>
        /// <exception cref="Exception"></exception>
        public void Connect(IPEndPoint ep)
        {
            if (connected)
            {
                return;
            }

            var socket = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.KeepAlive();

            AsyncServerUserToken token = new AsyncServerUserToken
            {
                TargetSocket = socket,
            };
            SocketAsyncEventArgs connectEventArgs = new SocketAsyncEventArgs
            {
                UserToken = token,
                SocketFlags = SocketFlags.None,
            };
            connectEventArgs.RemoteEndPoint = ep;
            connectEventArgs.Completed += Target_IO_Completed;
            if (!socket.ConnectAsync(connectEventArgs))
            {
                TargetProcessConnect(connectEventArgs);
            }
        }

        private void Target_IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    TargetProcessConnect(e);
                    break;
                case SocketAsyncOperation.Receive:
                    TargetProcessReceive();
                    break;
                default:
                    break;
            }
        }
        private void TargetProcessConnect(SocketAsyncEventArgs connectEventArgs)
        {
            AsyncServerUserToken token = (AsyncServerUserToken)connectEventArgs.UserToken;
            try
            {
                if (connectEventArgs.SocketError == SocketError.Success)
                {

                    token.SecWebSocketKey = WebSocketParser.BuildSecWebSocketKey();
                    WebsocketHeaderInfo header = new WebsocketHeaderInfo();
                    header.SetHeaderValue(WebsocketHeaderKey.SecWebSocketKey, token.SecWebSocketKey);
                    byte[] connectData = WebSocketParser.BuildConnectData(header);

                    token.TargetSocket.Send(connectData, SocketFlags.None);

                    connected = true;
                    BindTargetReceive(token);
                }
                else
                {
                    OnConnectFail(connectEventArgs.SocketError.ToString());
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
                OnConnectFail(ex.Message);
            }
        }
        private void BindTargetReceive(AsyncServerUserToken connectToken)
        {
            AsyncServerUserToken token = new AsyncServerUserToken
            {
                TargetSocket = connectToken.TargetSocket,
                SecWebSocketKey = connectToken.SecWebSocketKey,
            };
            try
            {
                readEventArgs = new SocketAsyncEventArgs
                {
                    UserToken = token,
                    SocketFlags = SocketFlags.None,
                };
                token.PoolBuffer = new byte[bufferSize];
                readEventArgs.SetBuffer(token.PoolBuffer, 0, bufferSize);
                readEventArgs.Completed += Target_IO_Completed;
                if (token.TargetSocket.ReceiveAsync(readEventArgs) == false)
                {
                    TargetProcessReceive();
                }
            }
            catch (Exception ex)
            {
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
        }
        private void TargetProcessReceive()
        {
            try
            {
                AsyncServerUserToken token = (AsyncServerUserToken)readEventArgs.UserToken;
                if (readEventArgs.BytesTransferred > 0 && readEventArgs.SocketError == SocketError.Success)
                {
                    int offset = readEventArgs.Offset;
                    int length = readEventArgs.BytesTransferred;

                    ReadFrame(token, readEventArgs.Buffer.AsMemory(offset, length));
                    if (token.TargetSocket.Available > 0)
                    {
                        while (token.TargetSocket.Available > 0)
                        {
                            length = token.TargetSocket.Receive(readEventArgs.Buffer);
                            if (length > 0)
                            {
                                ReadFrame(token, readEventArgs.Buffer.AsMemory(0, length));
                            }
                        }
                    }

                    if (token.TargetSocket.Connected == false)
                    {
                        CloseClientSocket();
                        return;
                    }
                    if (token.TargetSocket.ReceiveAsync(readEventArgs) == false)
                    {
                        TargetProcessReceive();
                    }
                }
                else
                {
                    CloseClientSocket();
                }
            }
            catch (Exception ex)
            {
                CloseClientSocket();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error(ex);
            }
        }

        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            if (connected)
            {
                AsyncServerUserToken token = readEventArgs.UserToken as AsyncServerUserToken;
                token.Clear();
                readEventArgs.Dispose();
                OnDisConnectd();
            }
            connected = false;
            connecSuccess = false;
        }
        private void CloseClientSocket()
        {
            CloseClientSocket(readEventArgs);
        }
        /// <summary>
        /// 
        /// </summary>
        public void Close()
        {
            CloseClientSocket();
        }


        private readonly Dictionary<WebSocketFrameInfo.EnumOpcode, Action<AsyncServerUserToken>> handles;
        private void ReadFrame(AsyncServerUserToken token, Memory<byte> data)
        {
            if (connecSuccess)
            {
                if (token.FrameBuffer.Size == 0 && data.Length > 6)
                {
                    if (WebSocketFrameInfo.TryParse(data, out token.FrameInfo))
                    {
                        ExecuteHandle(token);
                        if (token.FrameInfo.TotalLength == data.Length)
                        {
                            return;
                        }
                        token.FrameBuffer.AddRange(data.Slice(token.FrameInfo.TotalLength));
                    }
                    else
                    {
                        token.FrameBuffer.AddRange(data);
                    }
                }
                else
                {
                    token.FrameBuffer.AddRange(data);
                }

                do
                {
                    if (!WebSocketFrameInfo.TryParse(token.FrameBuffer.Data.Slice(token.FrameIndex), out token.FrameInfo))
                    {
                        break;
                    }
                    if (token.FrameInfo.Fin == WebSocketFrameInfo.EnumFin.Fin)
                    {
                        ExecuteHandle(token);
                        token.FrameBuffer.RemoveRange(0, token.FrameInfo.TotalLength);
                        token.FrameIndex = 0;
                    }
                    else
                    {
                        token.FrameBuffer.RemoveRange(token.FrameIndex, token.FrameInfo.TotalLength - token.FrameInfo.PayloadData.Length);
                        token.FrameIndex += token.FrameInfo.PayloadData.Length;
                    }
                } while (token.FrameBuffer.Size > 6);
            }
            else
            {
                HandleConnect(token, data);
            }
        }
        private void ExecuteHandle(AsyncServerUserToken token)
        {
            if (handles.TryGetValue(token.FrameInfo.Opcode, out Action<AsyncServerUserToken> action))
            {
                action(token);
            }
            else if (token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.UnControll3 && token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.UnControll7)
            {
                OnUnControll(token.FrameInfo);
            }
            else if (token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.Controll11 && token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.Controll15)
            {
                OnControll(token.FrameInfo);
            }
            else
            {
                SendFrameClose(WebSocketFrameInfo.EnumCloseStatus.Unexpected);
                CloseClientSocket();
                return;
            }
        }
        private void HandleData(AsyncServerUserToken token)
        {
            token.Opcode = token.FrameInfo.Opcode;
            HandleAppendData(token);
        }
        private void HandleAppendData(AsyncServerUserToken token)
        {
            if (token.FrameInfo.Fin == WebSocketFrameInfo.EnumFin.Fin)
            {
                if (token.Opcode == WebSocketFrameInfo.EnumOpcode.Text)
                {
                    string str = token.FrameInfo.PayloadData.GetString();
                    OnMessage(token.FrameInfo, str);
                }
                else
                {
                    OnBinary(token.FrameInfo, token.FrameInfo.PayloadData);
                }
            }
        }
        private void HandleClose(AsyncServerUserToken token)
        {
            SendFrameClose(WebSocketFrameInfo.EnumCloseStatus.Normal);
            CloseClientSocket();
            OnClose();
        }
        private void HandlePing(AsyncServerUserToken token)
        {
            SendFramePong();
        }
        private void HandlePong(AsyncServerUserToken token) { }
        private void HandleConnect(AsyncServerUserToken token, Memory<byte> data)
        {
            WebsocketHeaderInfo header = WebsocketHeaderInfo.Parse(data);
            if (header.TryGetHeaderValue(WebsocketHeaderKey.SecWebSocketAccept, out string accept) == false || WebSocketParser.VerifySecWebSocketAccept(token.SecWebSocketKey, accept) == false)
            {
                OnConnectFail("Sec-WebSocket-Accept Invalid");
                CloseClientSocket();
                return;
            }
            if (header.StatusCode != HttpStatusCode.SwitchingProtocols)
            {
                OnConnectFail($"{(int)header.StatusCode},{header.StatusCode}");
                CloseClientSocket();
                return;
            }
            if (!OnConnecting(header))
            {
                CloseClientSocket();
                return;
            }

            connecSuccess = true;
            OnOpen(header);
        }
        public int SendRaw(byte[] buffer)
        {
            var socket = (readEventArgs.UserToken as AsyncServerUserToken).TargetSocket;

            return socket.Send(buffer, SocketFlags.None);
        }
        public int SendRaw(Memory<byte> buffer)
        {
            var socket = (readEventArgs.UserToken as AsyncServerUserToken).TargetSocket;

            return socket.Send(buffer.Span, SocketFlags.None);
        }
        public int SendFrame(WebSocketFrameRemarkInfo remark)
        {
            remark.Mask = WebSocketFrameInfo.EnumMask.Mask;
            remark.MaskData = WebSocketParser.BuildMaskKey();
            var frame = WebSocketParser.BuildFrameData(remark, out int length);
            int res = SendRaw(frame.AsMemory(0, length));
            WebSocketParser.Return(frame);
            return res;
        }
        public int SendFrameText(string txt)
        {
            return SendFrameText(txt.ToBytes());
        }
        public int SendFrameText(byte[] buffer)
        {
            return SendFrame(new WebSocketFrameRemarkInfo
            {
                Opcode = WebSocketFrameInfo.EnumOpcode.Text,
                Data = buffer
            });
        }
        public int SendFrameBinary(byte[] buffer)
        {
            return SendFrame(new WebSocketFrameRemarkInfo
            {
                Opcode = WebSocketFrameInfo.EnumOpcode.Binary,
                Data = buffer
            });
        }
        public int SendFramePoing()
        {
            return SendRaw(WebSocketParser.BuildPingData());
        }
        public int SendFramePong()
        {
            return SendRaw(WebSocketParser.BuildPongData());
        }
        public int SendFrameClose(WebSocketFrameInfo.EnumCloseStatus status)
        {
            return SendFrame(new WebSocketFrameRemarkInfo
            {
                Opcode = WebSocketFrameInfo.EnumOpcode.Close,
                Data = ((ushort)status).ToBytes()
            });
        }
        public void Dispose()
        {
            CloseClientSocket();
            GC.Collect();
            //GC.SuppressFinalize(this);
        }
    }
    public sealed class AsyncServerUserToken
    {
        public Socket TargetSocket { get; set; }

        /// <summary>
        /// 当前帧数据
        /// </summary>
        public WebSocketFrameInfo FrameInfo;
        /// <summary>
        /// 当前帧的数据下标
        /// </summary>
        public int FrameIndex { get; set; }
        /// <summary>
        /// 数据帧缓存
        /// </summary>
        public ReceiveDataBuffer FrameBuffer { get; } = new ReceiveDataBuffer();
        /// <summary>
        /// 当前帧的数据类型
        /// </summary>
        public WebSocketFrameInfo.EnumOpcode Opcode { get; set; }
        public string SecWebSocketKey { get; set; }
        public byte[] PoolBuffer { get; set; }

        public void Clear()
        {
            TargetSocket?.SafeClose();
            TargetSocket = null;

            PoolBuffer = Helper.EmptyArray;

            //GC.Collect();
            //GC.SuppressFinalize(this);
        }
    }
}
