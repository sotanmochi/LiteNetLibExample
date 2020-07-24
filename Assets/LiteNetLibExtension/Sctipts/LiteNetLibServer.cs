// Copyright (c) 2020 Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LiteNetLibExtension.Server
{
    public delegate void OnPeerConnectedDelegate(int clientId);
    public delegate void OnPeerDisconnectedDelegate(int clientId);
    public delegate void OnNetworkEventReceivedDelegate(byte dataType, NetDataReader dataReader, int clientId);

    public class LiteNetLibServer : MonoBehaviour, INetEventListener
    {
        public LiteNetLibConfig Config;

        int _Port = 11010;
        string _Key = "";

        NetManager _ServerNetManager;
        Dictionary<int, NetPeer> _ConnectedClients;

        public event OnPeerConnectedDelegate OnPeerConnectedHandler;
        public event OnPeerDisconnectedDelegate OnPeerDisconnectedHandler;
        public event OnNetworkEventReceivedDelegate OnNetworkEventReceived;

        void Awake()
        {
            _ServerNetManager = new NetManager(this);
            _ConnectedClients = new Dictionary<int, NetPeer>();
        }

        void Update()
        {
            if (_ServerNetManager.IsRunning)
            {
                _ServerNetManager.PollEvents();
            }
        }

        void LateUpdate()
        {
            // Send queue
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
                Debug.LogError("Client[" + clientId + "] has not connected. @LiteNetLibServer.SendData()");
                Console.WriteLine("Client[" + clientId + "] has not connected. @LiteNetLibServer.SendData()");
            }
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            int clientId = LiteNetLibUtil.Peer2ClientId(peer);
            if (!_ConnectedClients.ContainsKey(clientId))
            {
                _ConnectedClients.Add(clientId, peer);
            }

            Debug.Log("OnPeerConnected.clientId: " + clientId);

            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put(NetworkDataType.OnConnectedServer);
            dataWriter.Put(clientId);
            peer.Send(dataWriter, DeliveryMethod.ReliableOrdered);

            OnPeerConnectedHandler?.Invoke(clientId);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine("OnPeerDisconnected : " + peer.EndPoint.Address + " : " + peer.EndPoint.Port + " Reason : " + disconnectInfo.Reason.ToString());

            int clientId = LiteNetLibUtil.Peer2ClientId(peer);
            if (_ConnectedClients.ContainsKey(clientId))
            {
                _ConnectedClients.Remove(clientId);
            }

            OnPeerDisconnectedHandler?.Invoke(clientId);
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.WriteLine("OnNetworkError : " + socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
           if (reader.UserDataSize >= 1)
            {
                byte dataType = reader.GetByte();
                int clientId = LiteNetLibUtil.Peer2ClientId(peer);
                OnNetworkEventReceived?.Invoke(dataType, reader, clientId);
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
