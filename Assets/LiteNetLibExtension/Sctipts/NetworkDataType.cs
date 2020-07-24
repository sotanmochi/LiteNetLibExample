// Copyright (c) 2020 Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace LiteNetLibExtension
{
    public partial class NetworkDataType
    {
        public static readonly byte OnConnectedServer = 0;
        public static readonly byte CreateRoom = 1;
        public static readonly byte OnCreatedRoom = 2;
        public static readonly byte JoinRoom = 3;
        public static readonly byte OnJoinedRoom = 4;
        public static readonly byte LeaveRoom = 5;
        public static readonly byte OnLeftRoom = 6;
        public static readonly byte OnPlayerLeftRoom = 7;
    }
}
