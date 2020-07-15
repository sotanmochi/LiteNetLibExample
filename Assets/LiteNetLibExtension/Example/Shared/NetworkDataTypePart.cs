// Copyright (c) 2020 Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace LiteNetLibExtension
{
    public partial class NetworkDataType
    {
        public static readonly byte PlayerTransform = 10;
    }

    public class NetworkDataSize
    {
        public static readonly int MaxNetworkObjectID = 1000;
        public static readonly int ActorIdAndTransform = sizeof(int) + sizeof(float) * 7;
        public static readonly int Transform = sizeof(float) * 7;
    }
}
