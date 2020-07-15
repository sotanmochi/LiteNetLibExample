// Copyright (c) 2020 Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using UnityEngine;
using LiteNetLib;
using LiteNetLib.Utils;

namespace LiteNetLibExtension.Example.Client
{
    public class GameClient : MonoBehaviour
    {
        [SerializeField] MultiplayerClient _MultiplayerClient;

        Dictionary<int, GameObject> _NetworkObjectDictionary;
        int _LastUsedSubId = 0;

        void Start()
        {
            _MultiplayerClient.OnNetworkReceived += OnNetworkReceivedHandler;
            _MultiplayerClient.OnLeftRoom += OnLeftRoom;
            _MultiplayerClient.OnConnectedServer += OnConnectedServer;
            _MultiplayerClient.OnDisconnectedServer += OnDisconnectedServer;

            _NetworkObjectDictionary = new Dictionary<int, GameObject>();
        }

        public void NetworkInstantiate(string prefabName, Vector3 position, Quaternion rotation)
        {
            int newObjectId = 0;
            int subId = _LastUsedSubId;
            int localActorId = _MultiplayerClient.LocalActorId;
            int ownerIdOffset = NetworkDataSize.MaxNetworkObjectID * localActorId;
            for (int i = 1; i <= NetworkDataSize.MaxNetworkObjectID; i++)
            {
                subId = (subId + 1) % NetworkDataSize.MaxNetworkObjectID;
                if (subId == 0)
                {
                    continue;   // avoid using subID 0
                }

                newObjectId = subId + ownerIdOffset;
                if (!_NetworkObjectDictionary.ContainsKey(newObjectId))
                {
                    _LastUsedSubId = newObjectId;
                    break;
                }
            }

            GameObject prefab = Resources.Load<GameObject>(prefabName);
            _NetworkObjectDictionary[newObjectId] = Instantiate(prefab, position, rotation);
            _NetworkObjectDictionary[newObjectId].name = newObjectId.ToString();

            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put(NetworkDataType.NetworkInstantiate);
            dataWriter.Put(newObjectId);
            dataWriter.Put(prefabName);
            dataWriter.Put(position.x);
            dataWriter.Put(position.y);
            dataWriter.Put(position.z);
            dataWriter.Put(rotation.x);
            dataWriter.Put(rotation.y);
            dataWriter.Put(rotation.z);
            dataWriter.Put(rotation.w);

            _MultiplayerClient.SendData(dataWriter, DeliveryMethod.ReliableOrdered);
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
                OnNetworkInstantiate(objectId, prefabName, posX, posY, posZ, rotX, rotY, rotZ, rotW);
            }
            if (networkDataType == NetworkDataType.RemoveNetworkObjects)
            {
                int actorId = reader.GetInt();
                OnRemoveNetworkObjects(actorId);
            }
        }

        void OnConnectedServer(int actorId)
        {
            Debug.Log("OnConnectedServer@GameClient: " + actorId);
        }

        void OnDisconnectedServer()
        {
            Debug.Log("OnDisconnectedServer@GameClient");
        }

        void OnLeftRoom(int actorId)
        {
            OnRemoveNetworkObjects(actorId);
        }

        void OnNetworkInstantiate(int objectId, string prefabName, float posX, float posY, float posZ, float rotX, float rotY, float rotZ, float rotW)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            _NetworkObjectDictionary[objectId] = Instantiate(prefab, new Vector3(posX, posY, posZ), new Quaternion(rotX, rotY, rotZ, rotW));
        }

        void OnRemoveNetworkObjects(int actorId)
        {
            int ownerOffset = NetworkDataSize.MaxNetworkObjectID * actorId;
            int ownerEnd = NetworkDataSize.MaxNetworkObjectID * (actorId + 1);
            for (int i = ownerOffset; i < ownerEnd; i++)
            {
                GameObject go;
                if (_NetworkObjectDictionary.TryGetValue(i, out go))
                {
                    _NetworkObjectDictionary.Remove(i);
                    Destroy(go);
                }
            }
        }
    }
}
