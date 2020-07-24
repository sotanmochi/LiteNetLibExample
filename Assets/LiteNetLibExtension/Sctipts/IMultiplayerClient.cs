
using LiteNetLib.Utils;

namespace LiteNetLibExtension
{
    public delegate void OnConnectedToServerDelegate(int actorId);
    public delegate void OnCreatedRoomDelegate(string groupName);
    public delegate void OnJoinedRoomDelegate(int actorId, string userName, string groupName);
    public delegate void OnLeftRoomDelegate();
    public delegate void OnPlayerLeftRoomDelegate(int actorId);

    public interface IMultiplayerClient
    {
        void SendData(NetDataWriter dataWriter);
    }
}