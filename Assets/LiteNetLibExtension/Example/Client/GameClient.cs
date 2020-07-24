// Copyright (c) 2020 Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LiteNetLib.Utils;

namespace LiteNetLibExtension.Example.Client
{
    public class GameClient : MonoBehaviour
    {
        [SerializeField] MultiplayerClient _MultiplayerClient;

        Dictionary<int, GameObject> _LocalObjectDictionary;
        Dictionary<int, GameObject> _NetworkObjectDictionary;
        int _LastUsedSubId = 0;

        void Start()
        {
            _MultiplayerClient.OnNetworkEventReceivedHandler += OnNetworkEventReceived;
            _MultiplayerClient.OnLeftRoomHandler += OnLeftRoom;
            // _MultiplayerClient.OnPlayerLeftRoomHandler += OnPlayerLeftRoom;
            _MultiplayerClient.OnConnectedToServerHandler += OnConnectedServer;
            _MultiplayerClient.OnDisconnectedServerHandler += OnDisconnectedServer;

            _LocalObjectDictionary = new Dictionary<int, GameObject>();
            _NetworkObjectDictionary = new Dictionary<int, GameObject>();
        }

        void Update()
        {
            SendPose();
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
                    continue; // avoid using subID 0
                }

                newObjectId = subId + ownerIdOffset;
                if (!_LocalObjectDictionary.ContainsKey(newObjectId))
                {
                    _LastUsedSubId = newObjectId;
                    break;
                }
            }

            GameObject prefab = Resources.Load<GameObject>(prefabName);
            _LocalObjectDictionary[newObjectId] = Instantiate(prefab, position, rotation);
            _LocalObjectDictionary[newObjectId].name = newObjectId.ToString();

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

            Debug.Log("NetworkInstantiate @GameClient");
            _MultiplayerClient.SendData(dataWriter);
            Debug.Log("Sent InstantiateData @GameClient");
        }

        public void SendPose()
        {
            if (!_MultiplayerClient.Joined || _LocalObjectDictionary.Count < 1)
            {
                return;
            }

            NetDataWriter dataWriter = new NetDataWriter();
            dataWriter.Put(NetworkDataType.UpdateObjectPose);
            dataWriter.Put(_MultiplayerClient.GroupName);

            int dataNum = _LocalObjectDictionary.Count;
            dataWriter.Put(dataNum);

            foreach (var localObject in _LocalObjectDictionary)
            {
                int id = localObject.Key;
                Vector3 position = localObject.Value.transform.position;
                Quaternion rotation = localObject.Value.transform.rotation;

                dataWriter.Put(id);
                dataWriter.Put(position.x);
                dataWriter.Put(position.y);
                dataWriter.Put(position.z);
                dataWriter.Put(rotation.x);
                dataWriter.Put(rotation.y);
                dataWriter.Put(rotation.z);
                dataWriter.Put(rotation.w);
            }

            Debug.Log("SendPose @GameClient");
            _MultiplayerClient.SendData(dataWriter);
            Debug.Log("Sent PoseData @GameClient");
        }

        void OnNetworkEventReceived(byte networkDataType, NetDataReader reader)
        {
            Debug.Log("OnNetworkReceived@GameClient");
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
                int dataNum = reader.GetInt();

                List<int> objectIdList = new List<int>(dataNum);
                for (int k = 0; k < dataNum; k++)
                {
                    int objectId = reader.GetInt();
                    objectIdList.Add(objectId);
                }

                OnRemoveNetworkObjects(objectIdList);
            }
            if (networkDataType == NetworkDataType.UpdateObjectPose)
            {
                int dataNum = reader.GetInt();
                Debug.Log("dataNum: " + dataNum);

                Vector3 position = Vector3.zero;
                Quaternion rotation = Quaternion.identity;
                for (int k = 0; k < dataNum; k++)
                {
                    int objectId = reader.GetInt();
                    position.x = reader.GetFloat();
                    position.y = reader.GetFloat();
                    position.z = reader.GetFloat();
                    rotation.x = reader.GetFloat();
                    rotation.y = reader.GetFloat();
                    rotation.z = reader.GetFloat();
                    rotation.w = reader.GetFloat();

                    if (_NetworkObjectDictionary.ContainsKey(objectId))
                    {
                        Debug.Log("Id: " + objectId + ", Pos: " + position);
                        _NetworkObjectDictionary[objectId].transform.SetPositionAndRotation(position, rotation);
                    }
                }
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

        void OnLeftRoom()
        {
            OnRemoveLocalObjects();
        }

        void OnNetworkInstantiate(int objectId, string prefabName, float posX, float posY, float posZ, float rotX, float rotY, float rotZ, float rotW)
        {
            GameObject prefab = Resources.Load<GameObject>(prefabName);
            _NetworkObjectDictionary[objectId] = Instantiate(prefab, new Vector3(posX, posY, posZ), new Quaternion(rotX, rotY, rotZ, rotW));
            _NetworkObjectDictionary[objectId].name = objectId + "(clone)";
        }

        void OnRemoveLocalObjects()
        {
            List<int> keyList = _LocalObjectDictionary.Keys.ToList();
            foreach (int key in keyList)
            {
                GameObject go;
                if (_LocalObjectDictionary.TryGetValue(key, out go))
                {
                    _LocalObjectDictionary.Remove(key);
                    Destroy(go);
                }
            }
            _LastUsedSubId = 0;
        }

        void OnRemoveNetworkObjects(List<int> objectIdList)
        {
            foreach (int id in objectIdList)
            {
                GameObject go;
                if (_NetworkObjectDictionary.TryGetValue(id, out go))
                {
                    _NetworkObjectDictionary.Remove(id);
                    Destroy(go);
                }
            }
        }
    }
}
