// Copyright (c) 2020 Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LiteNetLibExtension
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

    public class MultiplayerServer : MonoBehaviour
    {
        [SerializeField] LiteNetLibServer _LiteNetLibServer;
        public LiteNetLibServer LiteNetLibServer => _LiteNetLibServer;
        public OnNetworkReceiveDelegate OnNetworkReceived;

        Dictionary<int, Actor> _Actors = new Dictionary<int, Actor>();
        Dictionary<string, Group> _Groups = new Dictionary<string, Group>();

        public void StartServer()
        {
            _LiteNetLibServer.OnNetworkReceived += OnNetworkReceived;
            _LiteNetLibServer.OnNetworkReceived += OnNetworkReceivedHandler;
            _LiteNetLibServer.OnPeerDisconnectedHandler += OnPeerDisconnectedHandler;
            _LiteNetLibServer.StartServer();
        }

        public void SendToGroup(int senderId, NetDataWriter dataWriter, DeliveryMethod deliveryMethod)
        {
            if (_Actors.ContainsKey(senderId))
            {
                string groupName = _Actors[senderId].GroupName;
                foreach (int actorId in _Groups[groupName].Actors)
                {
                    _LiteNetLibServer.SendData(actorId, dataWriter, deliveryMethod);
                }
            }
        }

        public void SendToGroupExceptSelf(int senderId, NetDataWriter dataWriter, DeliveryMethod deliveryMethod)
        {
            if (_Actors.ContainsKey(senderId))
            {
                string groupName = _Actors[senderId].GroupName;
                foreach (int actorId in _Groups[groupName].Actors)
                {
                    if (actorId != senderId)
                    {
                        _LiteNetLibServer.SendData(actorId, dataWriter, deliveryMethod);
                    }
                }
            }
        }

        void OnNetworkReceivedHandler(byte networkDataType, NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (networkDataType == NetworkDataType.CreateRoom)
            {
                Debug.Log("CrateRoom");
                int actorId = LiteNetLibUtil.Peer2ClientId(peer);
                string groupName = reader.GetString();
                CreateRoom(actorId, groupName);
            }
            if (networkDataType == NetworkDataType.JoinRoom)
            {
                Debug.Log("JoinRoom");
                int actorId = LiteNetLibUtil.Peer2ClientId(peer);
                string userName = reader.GetString();
                string groupName = reader.GetString();
                JoinRoom(actorId, userName, groupName);
            }
            if (networkDataType == NetworkDataType.LeaveRoom)
            {
                Debug.Log("LeaveRoom");
                int actorId = LiteNetLibUtil.Peer2ClientId(peer);
                LeaveRoom(actorId);
            }
        }

        void OnPeerDisconnectedHandler(NetPeer peer)
        {
            int clientId = LiteNetLibUtil.Peer2ClientId(peer);
            LeaveRoom(clientId);
        }

        void CreateRoom(int actorId, string groupName)
        {
            if (!_Groups.ContainsKey(groupName))
            {
                Group group = new Group();
                group.Name = groupName;
                _Groups.Add(groupName, group);

                Debug.Log("CrateRoom");

                NetDataWriter dataWriter = new NetDataWriter();
                dataWriter.Put(NetworkDataType.OnCreatedRoom);
                dataWriter.Put(actorId);
                dataWriter.Put(groupName);
                _LiteNetLibServer.SendData(actorId, dataWriter, DeliveryMethod.ReliableOrdered);
            }
        }

        void JoinRoom(int actorId, string userName, string groupName)
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

            SendToGroup(actorId, dataWriter, DeliveryMethod.ReliableOrdered);
        }

        void LeaveRoom(int actorId)
        {
            if (_Actors.ContainsKey(actorId))
            {
                string groupName = _Actors[actorId].GroupName;
                Group group = _Groups[groupName];

                NetDataWriter dataWriter = new NetDataWriter();
                dataWriter.Put(NetworkDataType.OnLeftRoom);
                dataWriter.Put(actorId);
                SendToGroup(actorId, dataWriter, DeliveryMethod.ReliableOrdered);

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
