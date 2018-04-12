using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageProtocol
{
    public class PackType
    {
        // short = System.Int16
        public const short Command = 1; // 当成字符串处理
        public const short RequestTask = 2;
        public const short ResultModel = 3;
        public const short TaskModel = 4;
        public const short Rollback = 5;
    }
}
