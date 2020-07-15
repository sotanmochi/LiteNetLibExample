// Copyright (c) 2020 Soichiro Sugimoto.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using UnityEngine;

namespace LiteNetLibExtension
{
    [Serializable]
    [CreateAssetMenu(menuName = "LiteNetLib Extension/Create Config", fileName = "LiteNetLibConfig")]
    public class LiteNetLibConfig : ScriptableObject
    {
        public string Address = "localhost";
        public int Port = 11010;
        public string Key = "LiteNetLibExample";
    }
}