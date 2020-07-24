// Copyright (c) 2020 Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LiteNetLibExtension.Server
{
    public class Actor
    {
        public int ActorId;
        public string UserName;
        public string GroupName; 
    }

    public class Group
    {
        public string Name;
        public List<int> Actors = new List<int>();
    }

    public class MultiplayerServer : MonoBehaviour, IMultiplayerServer
    {
        [SerializeField] LiteNetLibServer _LiteNetLibServer;
        public LiteNetLibServer LiteNetLibServer => _LiteNetLibServer;

        public OnCreateRoomDelegate OnCreateRoom;
        public OnLeaveRoomDelegate OnLeaveRoom;
        public OnNetworkEventReceivedDelegate OnNetworkEventReceived;

        Dictionary<int, Actor> _Actors = new Dictionary<int, Actor>();
        Dictionary<string, Group> _Groups = new Dictionary<string, Group>();
        public Dictionary<int, Actor> Actors => _Actors;

        public void StartServer()
        {
            _LiteNetLibServer.OnNetworkEventReceived += OnNetworkEventReceivedHandler;
            _LiteNetLibServer.OnPeerDisconnectedHandler += OnPeerDisconnectedHandler;
            _LiteNetLibServer.StartServer();
        }

        public void SendData(int targetClientId, NetDataWriter dataWriter)
        {
            _LiteNetLibServer.SendData(targetClientId, dataWriter, DeliveryMethod.ReliableOrdered);
        }

        public void SendToGroup(string groupName, NetDataWriter dataWriter)
        {
            if (_Groups.ContainsKey(groupName))
            {
                foreach (int actorId in _Groups[groupName].Actors)
                {
                    _LiteNetLibServer.SendData(actorId, dataWriter, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        public void SendToGroup(int senderId, NetDataWriter dataWriter)
        {
            if (_Actors.ContainsKey(senderId))
            {
                string groupName = _Actors[senderId].GroupName;
                foreach (int actorId in _Groups[groupName].Actors)
                {
                    _LiteNetLibServer.SendData(actorId, dataWriter, DeliveryMethod.ReliableOrdered);
                }
            }
        }

        public void SendToGroupExceptSelf(int senderId, NetDataWriter dataWriter)
        {
            if (_Actors.ContainsKey(senderId))
            {
                string groupName = _Actors[senderId].GroupName;
                foreach (int actorId in _Groups[groupName].Actors)
                {
                    if (actorId != senderId)
                    {
                        _LiteNetLibServer.SendData(actorId, dataWriter, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
        }

        void OnNetworkEventReceivedHandler(byte networkDataType, NetDataReader dataReader, int clientId)
        {
            if (networkDataType == NetworkDataType.CreateRoom)
            {
                Debug.Log("CrateRoom");
                int actorId = clientId;
                string groupName = dataReader.GetString();
                CreateRoom(actorId, groupName);
            }
            if (networkDataType == NetworkDataType.JoinRoom)
            {
                Debug.Log("JoinRoom");
                int actorId = clientId;
                string userName = dataReader.GetString();
                string groupName = dataReader.GetString();
                JoinRoom(actorId, userName, groupName);
            }
            if (networkDataType == NetworkDataType.LeaveRoom)
            {
                Debug.Log("LeaveRoom");
                int actorId = clientId;
                LeaveRoom(actorId);
            }

            OnNetworkEventReceived?.Invoke(networkDataType, dataReader, clientId);
        }

        void OnPeerDisconnectedHandler(int clientId)
        {
            LeaveRoom(clientId);
        }

        public void CreateRoom(int actorId, string groupName)
        {
            Debug.Log("CreateRoom");
            if (!_Groups.ContainsKey(groupName))
            {
                Group group = new Group();
                group.Name = groupName;
                _Groups.Add(groupName, group);

                OnCreateRoom?.Invoke(actorId, groupName);

                NetDataWriter dataWriter = new NetDataWriter();
                dataWriter.Put(NetworkDataType.OnCreatedRoom);
                dataWriter.Put(actorId);
                dataWriter.Put(groupName);

                SendData(actorId, dataWriter);
                Debug.Log("OnCreatedRoom");
            }
        }

        public void JoinRoom(int actorId, string userName, string groupName)
        {
            if (_Actors.ContainsKey(actorId))
            {
                Debug.Log("Actor " + actorId + " has already joined.");
                return;
            }

            if (!_Groups.ContainsKey(groupName))
            {
                CreateRoom(actorId, groupName);
            }

            Actor actor = new Actor();
            actor.ActorId = actorId;
            actor.UserName = userName;
            actor.GroupName = groupName;

            _Groups[groupName].Actors.Add(actorId);
            _Actors[actorId] = actor;

            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put(NetworkDataType.OnJoinedRoom);
            dataWriter.Put(actorId);
            dataWriter.Put(userName);
            dataWriter.Put(groupName);

            SendData(actorId, dataWriter);
            Debug.Log("OnJoinedRoom");
        }

        public void LeaveRoom(int actorId)
        {
            if (_Actors.ContainsKey(actorId))
            {
                string groupName = _Actors[actorId].GroupName;
                Group group = _Groups[groupName];

                OnLeaveRoom?.Invoke(actorId);

                NetDataWriter dataWriter = new NetDataWriter();
                dataWriter.Put(NetworkDataType.OnLeftRoom);
                dataWriter.Put(actorId);
                SendData(actorId, dataWriter);
                Debug.Log("OnLeaveRoom");

                dataWriter.Reset();
                dataWriter.Put(NetworkDataType.OnPlayerLeftRoom);
                dataWriter.Put(actorId);
                SendToGroupExceptSelf(actorId, dataWriter);

                group.Actors.Remove(actorId);
                if (group.Actors.Count <= 0)
                {
                    _Groups.Remove(groupName);
                }

                _Actors.Remove(actorId);
            }
        }
    }
}
