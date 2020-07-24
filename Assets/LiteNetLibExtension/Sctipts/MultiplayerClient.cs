// Copyright (c) 2020 Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LiteNetLibExtension
{
    public class MultiplayerClient : MonoBehaviour, IMultiplayerClient
    {
        [SerializeField] LiteNetLibClient _LiteNetLibClient;

        public OnNetworkEventReceivedDelegate OnNetworkEventReceivedHandler;
        public OnConnectedToServerDelegate OnConnectedToServerHandler;
        public OnDisconnectedDelegate OnDisconnectedServerHandler;
        public OnCreatedRoomDelegate OnCreatedRoomHandler;
        public OnJoinedRoomDelegate OnJoinedRoomHandler;
        public OnLeftRoomDelegate OnLeftRoomHandler;
        public OnPlayerLeftRoomDelegate OnPlayerLeftRoomHandler;

        int _LocalActorId = -1;
        public int LocalActorId => _LocalActorId;
        string _LocalUserName;
        public string LocalUserName => _LocalUserName;
        string _GroupName;
        public string GroupName => _GroupName;

        bool _ConnectedServer = false;
        public bool ConnectedServer => _ConnectedServer;
        bool _Joined = false;
        public bool Joined => _Joined;

        bool _Initialized = false;

        void OnApplicationQuit()
        {
            LeaveRoom();
            _LiteNetLibClient.StopClient();
        }

        public void Initialize()
        {
            _LiteNetLibClient.OnNetworkEventReceived += OnNetworkEventReceived;
            _LiteNetLibClient.OnDisconnected += OnDisconnectedServer;
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

        public void SendData(NetDataWriter dataWriter)
        {
            {
                _LiteNetLibClient.SendData(dataWriter, DeliveryMethod.ReliableOrdered);
            }
        }

        public void CreateRoom(int actorId, string groupName)
        {
            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put(NetworkDataType.CreateRoom);
            dataWriter.Put(actorId);
            dataWriter.Put(groupName);
            SendData(dataWriter);
        }

        public void JoinRoom(string userName, string groupName)
        {
            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put(NetworkDataType.JoinRoom);
            dataWriter.Put(userName);
            dataWriter.Put(groupName);
            SendData(dataWriter);
        }

        public void LeaveRoom()
        {
            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put(NetworkDataType.LeaveRoom);
            dataWriter.Put(_LocalActorId);
            SendData(dataWriter);
        }

        void OnNetworkEventReceived(byte networkDataType, NetDataReader reader)
        {
            Debug.Log("OnNetworkReceived@MuliplayerClient");
            if (networkDataType == NetworkDataType.OnConnectedServer)
            {
                int actorId = reader.GetInt();
                OnConnectedServer(actorId);
            }
            if (networkDataType == NetworkDataType.OnCreatedRoom)
            {
                int actorId = reader.GetInt();
                string groupName = reader.GetString();
                OnCreatedRoom(groupName);
            }
            if (networkDataType == NetworkDataType.OnJoinedRoom)
            {
                Debug.Log("OnJoined @MuliplayerClient");
                int actorId = reader.GetInt();
                string userName = reader.GetString();
                string groupName = reader.GetString();
                OnJoinedRoom(actorId, userName, groupName);
            }
            if (networkDataType == NetworkDataType.OnLeftRoom)
            {
                int actorId = reader.GetInt();
                OnLeftRoom(actorId);
            }
            if (networkDataType == NetworkDataType.OnPlayerLeftRoom)
            {
                int actorId = reader.GetInt();
                OnPlayerLeftRoom(actorId);
            }

            Debug.Log("OnNetworkReceivedHandler.Invoke() @MuliplayerClient");
            OnNetworkEventReceivedHandler?.Invoke(networkDataType, reader);
        }

        void OnConnectedServer(int actorId)
        {
            _ConnectedServer = true;
            _Joined = false;
            _LocalActorId = actorId;

            Debug.Log("OnConnectedServer@MultiplayerClient");
            OnConnectedToServerHandler?.Invoke(actorId);
        }

        void OnDisconnectedServer()
        {
            _ConnectedServer = false;
            _Joined = false;
            _LocalActorId = -1;

            Debug.Log("OnDisconnectedServer@MultiplayerClient");
            OnDisconnectedServerHandler?.Invoke();
        }

        void OnCreatedRoom(string groupName)
        {
            Debug.Log("OnCreatedRoom: " + groupName + " @MultiplayerClient");            
            OnCreatedRoomHandler?.Invoke(groupName);
        }

        void OnJoinedRoom(int actorId, string userName, string groupName)
        {
            _Joined = true;
            _LocalActorId = actorId;
            _LocalUserName = userName;
            _GroupName = groupName;

            Debug.Log("OnJoinedRoom: " + ": " + actorId + ": " + userName + ": " + groupName + " @MultiplayerClient");
            OnJoinedRoomHandler?.Invoke(actorId, userName, groupName);
        }

        void OnLeftRoom(int actorId)
        {
            if (actorId == _LocalActorId)
            {
                _Joined = false;
                _LocalActorId = -1;
                _LocalUserName = "";
                _GroupName = "";

                Debug.Log("OnLeftRoom @MultiplayerClient");
                OnLeftRoomHandler?.Invoke();
            }
        }

        void OnPlayerLeftRoom(int actorId)
        {
            Debug.Log("OnPlayerLeftRoom");   
            OnPlayerLeftRoomHandler?.Invoke(actorId);
        }
    }
}
