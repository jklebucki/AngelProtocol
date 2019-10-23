using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AngelProtocol
{
    public class ComPortSettings
    {
        public string PortName { get; set; }
        public int BaudRate { get; set; }
        public int StopBits { get; set; }
        public int DataBits { get; set; }
        public int Parity { get; set; }
    }
}
