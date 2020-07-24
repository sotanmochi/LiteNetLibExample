
using LiteNetLib.Utils;

namespace LiteNetLibExtension
{
    public delegate void OnCreateRoomDelegate(int actorId, string groupName);
    public delegate void OnLeaveRoomDelegate(int actorId);

    public interface IMultiplayerServer
    {
        void StartServer();
        void SendData(int targetClientId, NetDataWriter dataWriter);
        void SendToGroup(string groupName, NetDataWriter dataWriter);
        void SendToGroup(int senderId, NetDataWriter dataWriter);
        void SendToGroupExceptSelf(int senderId, NetDataWriter dataWriter);
        void CreateRoom(int actorId, string groupName);
        void JoinRoom(int actorId, string userName, string groupName);
        void LeaveRoom(int actorId);
    }
}