
using LiteNetLib.Utils;

namespace LiteNetLibExtension
{
    public delegate void OnNetworkEventReceivedDelegate(byte dataType, NetDataReader dataReader);

    public interface INetworkEventHandler
    {
        void OnNetworkEventReceived(byte dataType, NetDataReader dataReader);
    }
}