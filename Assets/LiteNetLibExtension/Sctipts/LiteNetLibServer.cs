// Copyright (c) 2020 Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LiteNetLibExtension
{
    public delegate void OnPeerConnectedDelegate(NetPeer peer);
    public delegate void OnPeerDisconnectedDelegate(NetPeer peer);
    public delegate void OnNetworkReceiveDelegate(byte dataType, NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod);

    public class LiteNetLibServer : MonoBehaviour, INetEventListener
    {
        public LiteNetLibConfig Config;

        int _Port = 11010;
        string _Key = "";

        NetManager _ServerNetManager;
        Dictionary<int, NetPeer> _ConnectedClients;

        public OnPeerConnectedDelegate OnPeerConnectedHandler;
        public OnPeerDisconnectedDelegate OnPeerDisconnectedHandler;
        public OnNetworkReceiveDelegate OnNetworkReceived;

        void FixedUpdate()
        {
            if (_ServerNetManager.IsRunning)
            {
                _ServerNetManager.PollEvents();
            }
        }

        void OnApplicationQuit()
        {
            StopServer();
        }

        public void StartServer()
        {
            _Port = Config.Port;
            _Key = Config.Key;

            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                switch (args[i])
                {
                    case "--port" :
                        _Port = int.Parse(args[i + 1]);
                        break;
                    case "--key" :
                        _Key = args[i + 1];
                        break;
                }
            }

            _ServerNetManager = new NetManager(this);
            _ConnectedClients = new Dictionary<int, NetPeer>();

            if (_ServerNetManager.Start(_Port))
            {
                Console.WriteLine("LiteNetLib server started listening on port " + _Port);
            }
            else
            {
                Console.WriteLine("LiteNetLib server could not start!");
            }
        }

        public void StopServer()
        {
            if (_ServerNetManager != null && _ServerNetManager.IsRunning)
            {
                _ServerNetManager.Stop();
                Console.WriteLine("LiteNetLib server stopped.");
            }
            else
            {

            }
        }

        public void SendData(int clientId, NetDataWriter dataWriter, DeliveryMethod deliveryMethod)
        {
            if (_ConnectedClients.ContainsKey(clientId))
            {
                _ConnectedClients[clientId].Send(dataWriter, deliveryMethod);
            }
            else
            {
                
            }
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine("OnPeerConnected : " + peer.EndPoint.Address + " : " + peer.EndPoint.Port);

            int clientId = LiteNetLibUtil.Peer2ClientId(peer);
            if (!_ConnectedClients.ContainsKey(clientId))
            {
                _ConnectedClients.Add(clientId, peer);
            }

            Debug.Log("OnPeerConnected.clientId: " + clientId);
            Debug.Log("NetworkDataType: " + NetworkDataType.OnConnectedServer);
            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put(NetworkDataType.OnConnectedServer);
            dataWriter.Put(clientId);
            peer.Send(dataWriter, DeliveryMethod.ReliableOrdered);

            OnPeerConnectedHandler?.Invoke(peer);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine("OnPeerDisconnected : " + peer.EndPoint.Address + " : " + peer.EndPoint.Port + " Reason : " + disconnectInfo.Reason.ToString());

            int clientId = LiteNetLibUtil.Peer2ClientId(peer);
            if (_ConnectedClients.ContainsKey(clientId))
            {
                _ConnectedClients.Remove(clientId);
            }

            OnPeerDisconnectedHandler?.Invoke(peer);
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.WriteLine("OnNetworkError : " + socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
           if (reader.UserDataSize >= 4)
            {
                byte dataType = reader.GetByte();
                OnNetworkReceived?.Invoke(dataType, peer, reader, deliveryMethod);
            }
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            Console.WriteLine("OnNetworkReceiveUnconnected");
        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey(_Key);
        }
    }
}
