﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedKeyValueStore.NET
{
    internal static class Constants
    {
        public static readonly int N = 3;
        public static readonly int READ_QUORUM = 2;
        public static readonly int WRITE_QUORUM = 2;
    }
}