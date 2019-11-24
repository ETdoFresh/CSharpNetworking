﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpNetworking
{
    internal class Terminator
    {
        public const string VALUE = "\r\n";
        public const string HTTP = "\r\n\r\n";
        public const string CONSOLE = "{\\r\\n}";
        public readonly static byte[] BYTES = Encoding.UTF8.GetBytes("\r\n");
    }
}
