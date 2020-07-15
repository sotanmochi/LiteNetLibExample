// Copyright (c) 2020 Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LiteNetLibExtension
{
    public delegate void OnConnectedServerDelegate(int actorId);
    public delegate void OnCreatedRoomDelegate(string groupName);
    public delegate void OnJoinedRoomDelegate(int actorId, string userName, string groupName);
    public delegate void OnLeftRoomDelegate(int actorId);

    public class MultiplayerClient : MonoBehaviour
    {
        [SerializeField] LiteNetLibClient _LiteNetLibClient;
        public OnNetworkReceiveDelegate OnNetworkReceived;
        public OnConnectedServerDelegate OnConnectedServer;
        public OnDisconnectedServerDelegate OnDisconnectedServer;
        public OnCreatedRoomDelegate OnCreatedRoom;
        public OnJoinedRoomDelegate OnJoinedRoom;
        public OnLeftRoomDelegate OnLeftRoom;

        int _LocalActorId;
        public int LocalActorId => _LocalActorId;
        string _LocalUserName;
        public string LocalUserName => _LocalUserName;

        bool _Initialized = false;

        void OnApplicationQuit()
        {
            LeaveRoom();
            _LiteNetLibClient.StopClient();
        }

        public void Initialize()
        {
            _LiteNetLibClient.OnNetworkReceived += OnNetworkReceived;
            _LiteNetLibClient.OnNetworkReceived += OnNetworkReceivedHandler;
            _LiteNetLibClient.OnDisconnectedServer += OnDisconnectedServer;
            _LiteNetLibClient.OnDisconnectedServer += OnDisconnectedServerHandler;

            OnConnectedServer += OnConnectedServerHandler;
            OnCreatedRoom += OnCreatedRoomHandler;
            OnJoinedRoom += OnJoinedRoomHandler;
            OnLeftRoom += OnLeftRoomHandler;

            _Initialized = true;
        }

        public bool StartClient()
        {
            if (!_Initialized)
            {
                Initialize();
            }
            return _LiteNetLibClient.StartClient();
        }

        public void SendData(NetDataWriter dataWriter, DeliveryMethod deliveryMethod)
        {
            _LiteNetLibClient.SendData(dataWriter, deliveryMethod);
        }

        public void CreateRoom(int actorId, string groupName)
        {
            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put(NetworkDataType.CreateRoom);
            dataWriter.Put(actorId);
            dataWriter.Put(groupName);
            _LiteNetLibClient.SendData(dataWriter, DeliveryMethod.ReliableOrdered);
        }

        public void JoinRoom(string userName, string groupName)
        {
            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put(NetworkDataType.JoinRoom);
            dataWriter.Put(userName);
            dataWriter.Put(groupName);
            _LiteNetLibClient.SendData(dataWriter, DeliveryMethod.ReliableOrdered);
        }

        public void LeaveRoom()
        {
            Debug.Log("LeaveRoom");
            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put(NetworkDataType.LeaveRoom);
            dataWriter.Put(_LocalActorId);
            _LiteNetLibClient.SendData(dataWriter, DeliveryMethod.ReliableOrdered);
        }

        void OnNetworkReceivedHandler(byte networkDataType, NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            Debug.Log("NetworkDataType: " + networkDataType);
            if (networkDataType == NetworkDataType.OnConnectedServer)
            {
                int actorId = reader.GetInt();
                OnConnectedServer?.Invoke(actorId);
            }
            if (networkDataType == NetworkDataType.OnCreatedRoom)
            {
                int actorId = reader.GetInt();
                string groupName = reader.GetString();
                OnCreatedRoom?.Invoke(groupName);
            }
            if (networkDataType == NetworkDataType.OnJoinedRoom)
            {
                int actorId = reader.GetInt();
                string userName = reader.GetString();
                string groupName = reader.GetString();
                OnJoinedRoom?.Invoke(actorId, userName, groupName);
            }
            if (networkDataType == NetworkDataType.OnLeftRoom)
            {
                int actorId = reader.GetInt();
                OnLeftRoom?.Invoke(actorId);
            }
        }

        void OnConnectedServerHandler(int actorId)
        {
            Debug.Log("OnConnectedServerHandler@MultiplayerClient");
            _LocalActorId = actorId;
        }

        void OnDisconnectedServerHandler()
        {
            Debug.Log("OnDisconnectedServerHandler@MultiplayerClient");
        }

        void OnCreatedRoomHandler(string groupName)
        {
            Debug.Log("OnCreatedRoom: " + groupName);
        }

        void OnJoinedRoomHandler(int actorId, string userName, string groupName)
        {
            _LocalActorId = actorId;
            _LocalUserName = userName;
            Debug.Log("OnJoinedRoom: " + ": " + actorId + ": " + userName + ": " + groupName);
        }

        void OnLeftRoomHandler(int actorId)
        {
            Debug.Log("OnLeftRoom: " + actorId);
        }
    }
}
