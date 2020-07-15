using UnityEngine;
using UnityEngine.UI;

namespace LiteNetLibExtension.Example.Client
{
    public class DemoApp : MonoBehaviour
    {
        [SerializeField] MultiplayerClient _MultiplayerClient;
        [SerializeField] GameClient _GameClient;
        [SerializeField] GameObject _Prefab;

        [SerializeField] Button _ConnectServer;
        [SerializeField] Button _CreateRoom;
        [SerializeField] Button _JoinRoom;
        [SerializeField] Button _LeaveRoom;
        
        [SerializeField] Button _InstantiateObject;

        [SerializeField] InputField _RoomName;
        [SerializeField] InputField _PlayerName;

        void Start()
        {
            _ConnectServer.onClick.AddListener(OnClickConnectServer);
            _JoinRoom.onClick.AddListener(OnClickJoinRoom);
            _LeaveRoom.onClick.AddListener(OnClickLeaveRoom);
            _InstantiateObject.onClick.AddListener(OnClickInstantiate);
        }

        void OnClickConnectServer()
        {
            _MultiplayerClient.StartClient();
        }

        void OnClickJoinRoom()
        {
            _MultiplayerClient.JoinRoom(_PlayerName.text, _RoomName.text);
        }

        void OnClickLeaveRoom()
        {
            _MultiplayerClient.LeaveRoom();
        }

        void OnClickInstantiate()
        {
            _GameClient.NetworkInstantiate(_Prefab.name, Vector3.one, Quaternion.identity);
        }
    }
}
