// Copyright (c) 2020 Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LiteNetLibExtension.Example.Server
{
    public class GameServer : MonoBehaviour
    {
        [SerializeField] MultiplayerServer _MultiplayerServer;

        Dictionary<string, Dictionary<int, GameObject>> _NetworkObjectDictionary;

        void Start()
        {
            _MultiplayerServer.OnLeaveRoom += OnLeaveRoomHandler;
            _MultiplayerServer.LiteNetLibServer.OnNetworkReceived += OnNetworkReceivedHandler;
            _NetworkObjectDictionary = new Dictionary<string, Dictionary<int, GameObject>>();
            _MultiplayerServer.StartServer();
        }

        void LateUpdate()
        {
            SendNetworkObjectPose();
        }

        void OnNetworkReceivedHandler(byte networkDataType, NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            if (networkDataType == NetworkDataType.NetworkInstantiate)
            {               
                int objectId = reader.GetInt();
                string prefabName = reader.GetString();
                float posX = reader.GetFloat();
                float posY = reader.GetFloat();
                float posZ = reader.GetFloat();
                float rotX = reader.GetFloat();
                float rotY = reader.GetFloat();
                float rotZ = reader.GetFloat();
                float rotW = reader.GetFloat();

                int clientId = LiteNetLibUtil.Peer2ClientId(peer);
                NetworkInstantiate(clientId, objectId, prefabName, posX, posY, posZ, rotX, rotY, rotZ, rotW);
            }
            if (networkDataType == NetworkDataType.UpdateObjectPose)
            {
                string groupName = reader.GetString();
                int dataNum = reader.GetInt();

                if (!_NetworkObjectDictionary.ContainsKey(groupName))
                {
                    return;
                }

                Vector3 position = Vector3.zero;
                Quaternion rotation = Quaternion.identity;
                for (int i = 0; i < dataNum; i++)
                {
                    int objectId = reader.GetInt();
                    position.x = reader.GetFloat();
                    position.y = reader.GetFloat();
                    position.z = reader.GetFloat();
                    rotation.x = reader.GetFloat();
                    rotation.y = reader.GetFloat();
                    rotation.z = reader.GetFloat();
                    rotation.w = reader.GetFloat();

                    if (_NetworkObjectDictionary[groupName].ContainsKey(objectId))
                    {
                        _NetworkObjectDictionary[groupName][objectId].transform.SetPositionAndRotation(position, rotation);
                    }
                }
            }
        }

        void OnLeaveRoomHandler(int actorId)
        {
            RemoveNetworkObjects(actorId);
        }

        void SendNetworkObjectPose()
        {
            NetDataWriter dataWriter = new NetDataWriter();

            foreach (string groupName in _NetworkObjectDictionary.Keys)
            {
                dataWriter.Reset();
                dataWriter.Put(NetworkDataType.UpdateObjectPose);

                int dataNum = _NetworkObjectDictionary[groupName].Count;
                dataWriter.Put(dataNum);

                foreach (var networkObject in _NetworkObjectDictionary[groupName])
                {
                    int id = networkObject.Key;
                    Vector3 position = networkObject.Value.transform.position;
                    Quaternion rotation = networkObject.Value.transform.rotation;

                    dataWriter.Put(id);
                    dataWriter.Put(position.x);
                    dataWriter.Put(position.y);
                    dataWriter.Put(position.z);
                    dataWriter.Put(rotation.x);
                    dataWriter.Put(rotation.y);
                    dataWriter.Put(rotation.z);
                    dataWriter.Put(rotation.w);
                }

                _MultiplayerServer.SendToGroup(groupName, dataWriter, DeliveryMethod.ReliableOrdered);
            }
        }

        void NetworkInstantiate(int senderId, int objectId, string prefabName, float posX, float posY, float posZ, float rotX, float rotY, float rotZ, float rotW)
        {
            Debug.Log("NetworkInstantiate");
            string groupName = _MultiplayerServer.Actors[senderId].GroupName;

            if (!_NetworkObjectDictionary.ContainsKey(groupName))
            {
                _NetworkObjectDictionary[groupName] = new Dictionary<int, GameObject>();
            }

            GameObject prefab = Resources.Load<GameObject>(prefabName);
            _NetworkObjectDictionary[groupName][objectId] = Instantiate(prefab, new Vector3(posX, posY, posZ), new Quaternion(rotX, rotY, rotZ, rotW));
            _NetworkObjectDictionary[groupName][objectId].name = objectId.ToString();

            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put(NetworkDataType.NetworkInstantiate);
            dataWriter.Put(objectId);
            dataWriter.Put(prefabName);
            dataWriter.Put(posX);
            dataWriter.Put(posY);
            dataWriter.Put(posZ);
            dataWriter.Put(rotX);
            dataWriter.Put(rotY);
            dataWriter.Put(rotZ);
            dataWriter.Put(rotW);

            _MultiplayerServer.SendToGroupExceptSelf(senderId, dataWriter, DeliveryMethod.ReliableOrdered);
        }

        void RemoveNetworkObjects(int actorId)
        {
            string groupName = _MultiplayerServer.Actors[actorId].GroupName;

            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put(NetworkDataType.RemoveNetworkObjects);
            dataWriter.Put(actorId);

            int ownerOffset = NetworkDataSize.MaxNetworkObjectID * actorId;
            int ownerEnd = NetworkDataSize.MaxNetworkObjectID * (actorId + 1);

            int dataNum = ownerEnd - ownerOffset;
            dataWriter.Put(dataNum);

            Dictionary<int, GameObject> objectDictInGroup;
            if (_NetworkObjectDictionary.TryGetValue(groupName, out objectDictInGroup))
            {
                for (int objectId = ownerOffset; objectId < ownerEnd; objectId++)
                {
                    GameObject go;
                    if (objectDictInGroup.TryGetValue(objectId, out go))
                    {
                        dataWriter.Put(objectId);
                        objectDictInGroup.Remove(objectId);
                        Destroy(go);
                    }
                }
            }

            _MultiplayerServer.SendToGroupExceptSelf(actorId, dataWriter, DeliveryMethod.ReliableOrdered);
        }
    }
}
