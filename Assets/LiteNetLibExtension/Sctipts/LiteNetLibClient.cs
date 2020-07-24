// Copyright (c) 2020 Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Net.Sockets;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LiteNetLibExtension
{
    public delegate void OnDisconnectedDelegate();

    public class LiteNetLibClient : MonoBehaviour, INetEventListener
    {
        public LiteNetLibConfig Config;

        NetManager _ClientNetManager;
        NetPeer _ServerPeer;

        public event OnNetworkEventReceivedDelegate OnNetworkEventReceived;
        public event OnDisconnectedDelegate OnDisconnected;

        void Awake()
        {
            _ClientNetManager = new NetManager(this);
        }

        void Update()
        {
            if (_ClientNetManager.IsRunning)
            {
                _ClientNetManager.PollEvents();
            }
        }

        void LateUpdate()
        {
            // Send queues
        }

        public bool StartClient()
        {
            return StartClient(Config.Address, Config.Port);
        }

        public bool StartClient(string address, int port)
        {
            if (_ClientNetManager == null)
            {
                _ClientNetManager = new NetManager(this);
            }

            if (!_ClientNetManager.IsRunning)
            {
                _ClientNetManager.Start();
            }

            if (_ClientNetManager.IsRunning)
            {
                if (_ServerPeer != null)
                {
                    Debug.Log("LiteNetLib client has already started!");
                    return true;
                }

                Debug.Log("LiteNetLib client started!");
                _ClientNetManager.Connect(address, port, Config.Key);
                return true;
            }
            else
            {
                Debug.LogError("Could not start LiteNetLib client!");
                return false;
            }
        }

        public void StopClient()
        {
            if (_ClientNetManager != null && _ClientNetManager.IsRunning)
            {
                _ClientNetManager.Flush();
                _ClientNetManager.Stop();
                Debug.Log("LiteNetLib client stopped.");
            }
        }

        public void SendData(NetDataWriter dataWriter, DeliveryMethod deliveryMethod)
        {
            if (_ServerPeer != null)
            {
                _ServerPeer.Send(dataWriter, deliveryMethod);
            }
            else
            {
                Debug.LogError("Could not send data! Server peer is null!");
            }
        }

        void INetEventListener.OnPeerConnected(NetPeer peer)
        {
            _ServerPeer = peer;
            Debug.Log("OnPeerConnected : " + peer.EndPoint.Address + " : " + peer.EndPoint.Port);
        }

        void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            if ((_ServerPeer != null) && (peer.Id == _ServerPeer.Id))
            {
                _ServerPeer = null;
                OnDisconnected?.Invoke();
            }
            Debug.Log("OnPeerDisconnected : " + peer.EndPoint.Address + " : " + peer.EndPoint.Port + " Reason : " + disconnectInfo.Reason.ToString());
            Debug.Log("OnPeerDisconnected.Peer.Id : " + peer.Id);
        }

        void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Debug.LogError("OnNetworkError : " + socketError);
        }

        void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (reader.UserDataSize >= 1)
            {
                byte dataType = reader.GetByte();
                OnNetworkEventReceived?.Invoke(dataType, reader);
            }
        }

        void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            Debug.Log("OnNetworkReceiveUnconnected");
        }

        void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
        }

        void INetEventListener.OnConnectionRequest(ConnectionRequest request)
        {
        }
    }
}
