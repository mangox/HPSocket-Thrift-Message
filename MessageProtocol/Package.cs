using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MessageProtocol
{
    public class Package
    {
        public IntPtr ConnId { get; set; } // 服务端用的，表示客户端的连接句柄

        public short PackType { get; set; }
        public object Content { get; set; }
    }
}
