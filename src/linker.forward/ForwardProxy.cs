using linker.messenger.channel;
using linker.messenger.pcp;
using linker.messenger.signin;
using linker.tunnel;
using linker.tunnel.connection;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace linker.forward
{
    public partial class ForwardProxy : Channel, ITunnelConnectionReceiveCallback
    {
        private IForwardHook[] hooks = [];
        public string Error { get; private set; }
        public ForwardProxy(ISignInClientStore signInClientStore, TunnelTransfer tunnelTransfer,
            SignInClientTransfer signInClientTransfer, ChannelConnectionCaching channelConnectionCaching, IPcpStore pcpStore)
             : base(tunnelTransfer, signInClientTransfer, signInClientStore, channelConnectionCaching, pcpStore)
        {
            TaskUdp();
        }

        public virtual void Add(string machineId, IPEndPoint target, long recvBytes, long sendtBytes)
        {
        }
        public void Start(int port)
        {
            try
            {
                Start(new IPEndPoint(IPAddress.Any, port), 3);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }
        protected void Start(IPEndPoint ep, byte bufferSize)
        {
            StartTcp(ep, bufferSize);
            StartUdp(new IPEndPoint(ep.Address, ep.Port), bufferSize);
        }

        protected virtual IPAddress MapIp(IPAddress ip)
        {
            return ip;
        }
        protected virtual async ValueTask<int> Tunneling(AsyncUserToken token, ProtocolType protocol)
        {
            return 0;
        }
        private async ValueTask<bool> SendToConnection(AsyncUserToken token)
        {
            if (token.Connection == null)
            {
                return false;
            }
            await token.Connection.SendAsync(token.ReadPacket.Buffer.AsMemory(token.ReadPacket.Offset, token.ReadPacket.Length)).ConfigureAwait(false);
            Add(token.Connection.RemoteMachineId, token.IPEndPoint, token.ReadPacket.Length, 0);
            return true;
        }
        private async ValueTask<bool> SendToConnection(ITunnelConnection connection, ForwardReadPacket packet, IPEndPoint ep)
        {
            if (connection == null)
            {
                return false;
            }
            await connection.SendAsync(packet.Buffer.AsMemory(packet.Offset, packet.Length)).ConfigureAwait(false);
            Add(connection.RemoteMachineId, ep, packet.Length, 0);
            return true;
        }

        private async ValueTask InputPacket(ITunnelConnection connection, ReadOnlyMemory<byte> memory)
        {
            using ForwardWritePacket packet = new ForwardWritePacket(memory);

            if (packet.ProtocolType == ProtocolType.Tcp)
            {
                switch (packet.Flag)
                {
                    case ForwardFlags.Psh:
                        await HandlePshTcp(connection, packet, memory).ConfigureAwait(false);
                        break;
                    case ForwardFlags.PshAck:
                        await HandlePshAckTcp(connection, packet, memory).ConfigureAwait(false);
                        break;
                    case ForwardFlags.Syn:
                        _ = HandleSynTcp(connection, packet, memory);
                        break;
                    case ForwardFlags.SynAck:
                        HandleSynAckTcp(connection, packet);
                        break;
                    case ForwardFlags.Rst:
                        HandleRstTcp(connection, packet);
                        break;
                    case ForwardFlags.RstAck:
                        HandleRstAckTcp(connection, packet);
                        break;
                    default:
                        break;
                }
            }
            else if (packet.ProtocolType == ProtocolType.Udp)
            {
                switch (packet.Flag)
                {
                    case ForwardFlags.Psh:
                        await HndlePshUdp(connection, packet, memory).ConfigureAwait(false);
                        break;
                    case ForwardFlags.PshAck:
                        await HndlePshAckUdp(connection, packet, memory).ConfigureAwait(false);
                        break;
                    default:
                        break;
                }
            }
        }

        public void AddHooks(List<IForwardHook> hooks)
        {
            List<IForwardHook> list = this.hooks.ToList();
            list.AddRange(hooks);

            this.hooks = list.Distinct().ToArray();
        }
        private bool HookConnect(string srcId, IPEndPoint ep, ProtocolType protocol)
        {
            foreach (var hook in hooks)
            {
                if (hook.Connect(srcId, ep, protocol) == false)
                {
                    return false;
                }
            }
            return true;
        }
        private bool HookForward(AsyncUserToken token)
        {
            foreach (var hook in hooks)
            {
                if (hook.Forward(token) == false)
                {
                    return false;
                }
            }
            return true;
        }

        public void Stop()
        {
            try
            {
                StopTcp();
                StopUdp();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }
        public void Stop(int port)
        {
            StopTcp(port);
            StopUdp(port);
        }


        protected override void Connected(ITunnelConnection connection)
        {
            connection.BeginReceive(this, null);
        }
        public async ValueTask Receive(ITunnelConnection connection, ReadOnlyMemory<byte> memory, object userToken)
        {
            await InputPacket(connection, memory).ConfigureAwait(false);
        }
        public ValueTask Closed(ITunnelConnection connection, object userToken)
        {
            Version.Increment();
            return ValueTask.CompletedTask;
        }
    }
}
