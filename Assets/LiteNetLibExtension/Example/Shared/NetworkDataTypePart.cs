// Copyright (c) 2020 Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace LiteNetLibExtension
{
    public partial class NetworkDataType
    {
        public static readonly byte UpdateObjectPose = 10;
    }

    public class NetworkDataSize
    {
        public static readonly int MaxNetworkObjectID = 1000;
        public static readonly int IdAndPose = sizeof(int) + sizeof(float) * 7;
    }
}
