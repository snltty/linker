using linker.libs.extends;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace linker.libs.websocket
{
    /// <summary>
    /// websocket服务端
    /// </summary>
    public sealed class WebSocketServer
    {
        private Socket socket;
        private int BufferSize = 4 * 1024;

        private readonly ConcurrentDictionary<ulong, AsyncUserToken> connections = new();
        private readonly NumberSpaceUInt32 numberSpace = new NumberSpaceUInt32(0);

        /// <summary>
        /// 收到连接，可以在这处理 subProtocol extensions 及其它信息，false表示阻止连接，应设置header 的 StatusCode
        /// </summary>
        public Func<WebsocketConnection, WebsocketHeaderInfo, bool> OnConnecting = (connection, header) =>
        {
            header.SetHeaderValue(WebsocketHeaderKey.SecWebSocketExtensions, string.Empty); return true;
        };
        /// <summary>
        /// 已断开连接，没有收到关闭帧
        /// </summary>
        public Action<WebsocketConnection> OnDisConnectd = (connection) => { };

        /// <summary>
        /// 已连接
        /// </summary>
        public Action<WebsocketConnection> OnOpen = (connection) => { };
        /// <summary>
        /// 已关闭，收到关闭帧
        /// </summary>
        public Action<WebsocketConnection> OnClose = (connection) => { };

        /// <summary>
        /// 文本数据
        /// </summary>
        public Action<WebsocketConnection, WebSocketFrameInfo, string> OnMessage = (connection, frame, message) => { };
        /// <summary>
        /// 二进制数据
        /// </summary>
        public Action<WebsocketConnection, WebSocketFrameInfo, Memory<byte>> OnBinary = (connection, frame, data) => { };

        /// <summary>
        /// 控制帧，保留的控制帧，可以自定义处理
        /// </summary>
        public Action<WebsocketConnection, WebSocketFrameInfo> OnControll = (connection, frame) => { };
        /// <summary>
        /// 非控制帧，保留的非控制帧，可以自定义处理
        /// </summary>
        public Action<WebsocketConnection, WebSocketFrameInfo> OnUnControll = (connection, frame) => { };
        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<WebsocketConnection> Connections
        {
            get
            {
                return connections.Values.Select(c => c.Connectrion);
            }
        }
        public WebSocketServer()
        {
            handles = new Dictionary<WebSocketFrameInfo.EnumOpcode, Action<AsyncUserToken>> {
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
        public void Start(int port)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.IPv6Any, port);

            socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.IPv6Only(localEndPoint.AddressFamily, false);
            socket.Bind(localEndPoint);
            socket.Listen(int.MaxValue);

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs
            {
                UserToken = socket,
                SocketFlags = SocketFlags.None,
            };
            acceptEventArg.Completed += IO_Completed;
            StartAccept(acceptEventArg);
        }
        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Accept:
                    ProcessAccept(e);
                    break;
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    break;
            }
        }
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            acceptEventArg.AcceptSocket = null;
            Socket listenSocket = (Socket)acceptEventArg.UserToken;
            try
            {
                if (!listenSocket.AcceptAsync(acceptEventArg))
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.Instance.Error(ex);
            }
        }
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            BindReceive(e.AcceptSocket);
            StartAccept(e);
        }
        private void BindReceive(Socket socket)
        {
            socket.KeepAlive(10, 5);
            AsyncUserToken token = new AsyncUserToken
            {
                Connectrion = new WebsocketConnection { Socket = socket, Id = numberSpace.Increment() }
            };
            connections.TryAdd(token.Connectrion.Id, token);
            SocketAsyncEventArgs readEventArgs = new SocketAsyncEventArgs
            {
                UserToken = token,
                SocketFlags = SocketFlags.None,
            };
            token.PoolBuffer = new byte[BufferSize];
            readEventArgs.SetBuffer(token.PoolBuffer, 0, BufferSize);
            readEventArgs.Completed += IO_Completed;
            if (socket.ReceiveAsync(readEventArgs) == false)
            {
                ProcessReceive(readEventArgs);
            }
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            try
            {
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    var memory = e.Buffer.AsMemory(e.Offset, e.BytesTransferred);
                    ReadFrame(token, memory);
                    if (token.Connectrion.Socket.Available > 0)
                    {
                        while (token.Connectrion.Socket.Available > 0)
                        {
                            int length = token.Connectrion.Socket.Receive(e.Buffer);
                            if (length > 0)
                            {
                                memory = e.Buffer.AsMemory(0, length);
                                ReadFrame(token, memory);
                            }
                        }
                    }

                    if (!token.Connectrion.Socket.Connected)
                    {
                        CloseClientSocket(e);
                        return;
                    }
                    if (!token.Connectrion.Socket.ReceiveAsync(e))
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            catch (Exception)
            {
                CloseClientSocket(e);
            }
        }
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                if (!token.Connectrion.Socket.ReceiveAsync(e))
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;
            if (token.Disposabled == false)
            {
                e.Dispose();
                if (connections.TryRemove(token.Connectrion.Id, out _))
                {
                    token.Clear();
                }
                OnDisConnectd(token.Connectrion);
            }

        }
        public void Stop()
        {
            socket?.SafeClose();
            foreach (var item in connections.Values)
            {
                item.Clear();
            }
            connections.Clear();
        }


        private readonly Dictionary<WebSocketFrameInfo.EnumOpcode, Action<AsyncUserToken>> handles;
        /// <summary>
        /// 读取数据帧，分帧、粘包、半包处理，得到完整的包再根据opcode交给对应的处理
        /// </summary>
        /// <param name="token"></param>
        /// <param name="data"></param>
        private void ReadFrame(AsyncUserToken token, Memory<byte> data)
        {
            if (token.Connectrion.Connected)
            {
                if (token.FrameBuffer.Size == 0 && data.Length > 6)
                {
                    if (WebSocketFrameInfo.TryParse(data, out token.FrameInfo) && token.FrameInfo.Fin == WebSocketFrameInfo.EnumFin.Fin)
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
                    if (WebSocketFrameInfo.TryParse(token.FrameBuffer.Data.Slice(0, token.FrameBuffer.Size), out token.FrameInfo) == false)
                    {
                        break;
                    }
                    if (token.FrameInfo.Fin == WebSocketFrameInfo.EnumFin.Fin)
                    {
                        ExecuteHandle(token);
                        token.FrameBuffer.RemoveRange(0, token.FrameInfo.TotalLength);
                    }
                    else
                    {
                        token.FrameBuffer.RemoveRange(0, token.FrameBuffer.Size);
                        break;
                    }
                } while (token.FrameBuffer.Size > 6);
            }
            else
            {
                HandleConnect(token, data);
            }
        }
        private void ExecuteHandle(AsyncUserToken token)
        {
            if (handles.TryGetValue(token.FrameInfo.Opcode, out Action<AsyncUserToken> action))
            {
                action(token);
            }
            else if (token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.UnControll3 && token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.UnControll7)
            {
                OnUnControll(token.Connectrion, token.FrameInfo);
            }
            else if (token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.Controll11 && token.FrameInfo.Opcode >= WebSocketFrameInfo.EnumOpcode.Controll15)
            {
                OnControll(token.Connectrion, token.FrameInfo);
            }
            else
            {
                token.Connectrion.SendFrameClose(WebSocketFrameInfo.EnumCloseStatus.ExtendsError);
                token.Connectrion.Close();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error($"websocket opcode error:{token.FrameInfo.Opcode}");
                return;
            }
        }
        private void HandleData(AsyncUserToken token)
        {
            token.Opcode = token.FrameInfo.Opcode;
            HandleAppendData(token);
        }
        private void HandleAppendData(AsyncUserToken token)
        {
            if (token.FrameInfo.Fin == WebSocketFrameInfo.EnumFin.Fin)
            {
                if (token.Opcode == WebSocketFrameInfo.EnumOpcode.Text)
                {
                    string str = token.FrameInfo.PayloadData.GetString();
                    OnMessage(token.Connectrion, token.FrameInfo, str);
                }
                else
                {
                    OnBinary(token.Connectrion, token.FrameInfo, token.FrameInfo.PayloadData);
                }
            }
        }

        private void HandleClose(AsyncUserToken token)
        {
            token.Connectrion.SendFrameClose(WebSocketFrameInfo.EnumCloseStatus.Normal);
            token.Connectrion.Close();
            OnClose(token.Connectrion);
        }
        private void HandlePing(AsyncUserToken token)
        {
            token.Connectrion.SendFramePong();
        }
        private void HandlePong(AsyncUserToken token) { }
        private void HandleConnect(AsyncUserToken token, Memory<byte> data)
        {
            WebsocketHeaderInfo header = WebsocketHeaderInfo.Parse(data);
            if (header.TryGetHeaderValue(WebsocketHeaderKey.SecWebSocketKey, out string key) == false)
            {
                header.StatusCode = HttpStatusCode.MethodNotAllowed;
                token.Connectrion.ConnectResponse(header);
                token.Connectrion.Close();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error("websocket SecWebSocketKey error");
                return;
            }

            if (OnConnecting(token.Connectrion, header))
            {
                token.Connectrion.Connected = true;
                token.Connectrion.ConnectResponse(header);
                OnOpen(token.Connectrion);
            }
            else
            {
                header.StatusCode = HttpStatusCode.Unauthorized;
                token.Connectrion.ConnectResponse(header);
                token.Connectrion.Close();
                if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                    LoggerHelper.Instance.Error("websocket OnConnecting false");
            }
        }

    }

    public sealed class WebsocketConnection
    {
        public uint Id { get; set; }
        public Socket Socket { get; init; }
        public bool Connected { get; set; }
        public bool SocketConnected => Socket != null && Socket.Connected;

        private bool Closed;
        public int ConnectResponse(WebsocketHeaderInfo header)
        {
            var data = WebSocketParser.BuildConnectResponseData(header);
            return SendRaw(data);
        }
        public int SendRaw(byte[] buffer)
        {
            return Socket.Send(buffer, SocketFlags.None);
        }
        public int SendRaw(Memory<byte> buffer)
        {
            try
            {
                return Socket.Send(buffer.Span, SocketFlags.None);
            }
            catch (Exception)
            {
            }
            return 0;
        }
        public int SendFrame(WebSocketFrameRemarkInfo remark)
        {
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
        public int SendFrameBinary(Memory<byte> buffer)
        {
            return SendFrame(new WebSocketFrameRemarkInfo
            {
                Opcode = WebSocketFrameInfo.EnumOpcode.Binary,
                Data = buffer
            });
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
        public void Close()
        {
            if (!Closed)
            {
                Socket?.SafeClose();
            }
            Closed = true;
            Connected = false;
        }
    }

    public sealed class AsyncUserToken
    {
        public WebsocketConnection Connectrion { get; set; }

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
        public byte[] PoolBuffer { get; set; }
        public bool Disposabled { get; private set; }
        public void Clear()
        {
            Disposabled = true;
            Connectrion.Close();

            if (LoggerHelper.Instance.LoggerLevel <= LoggerTypes.DEBUG)
                LoggerHelper.Instance.Error($"websocket connection clear");
        }
    }

}
