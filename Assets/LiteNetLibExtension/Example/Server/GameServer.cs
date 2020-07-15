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

        Dictionary<int, GameObject> _NetworkObjectDictionary;

        void Start()
        {
            _MultiplayerServer.LiteNetLibServer.OnNetworkReceived += OnNetworkReceivedHandler;
            _NetworkObjectDictionary = new Dictionary<int, GameObject>();
            _MultiplayerServer.StartServer();
        }

        void OnNetworkReceivedHandler(byte networkDataType, NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            Debug.Log("OnNetworkReceived@GameServer");
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
            if (networkDataType == NetworkDataType.LeaveRoom)
            {
                Debug.Log("LeaveRoom@GameServer");
                int actorId = reader.GetInt();
                RemoveNetworkObjects(actorId);
            }
            if (networkDataType == NetworkDataType.PlayerTransform)
            {
                
            }
        }

        void NetworkInstantiate(int senderId, int objectId, string prefabName, float posX, float posY, float posZ, float rotX, float rotY, float rotZ, float rotW)
        {
            Debug.Log("NetworkInstantiate");

            GameObject prefab = Resources.Load<GameObject>(prefabName);
            _NetworkObjectDictionary[objectId] = Instantiate(prefab, new Vector3(posX, posY, posZ), new Quaternion(rotX, rotY, rotZ, rotW));
            _NetworkObjectDictionary[objectId].name = objectId.ToString();

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

            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put(NetworkDataType.RemoveNetworkObjects);
            dataWriter.Put(actorId);

            _MultiplayerServer.SendToGroupExceptSelf(actorId, dataWriter, DeliveryMethod.ReliableOrdered);
        }
    }
}
