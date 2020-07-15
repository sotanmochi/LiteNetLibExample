// Copyright (c) 2020 Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using LiteNetLib;

namespace LiteNetLibExtension
{
    public class LiteNetLibUtil
    {
        public static int Peer2ClientId(NetPeer peer)
        {
            return peer.Id + 1;
        }
    }
}