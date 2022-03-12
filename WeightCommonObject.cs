using System;
using System.IO.Ports;
using System.Text;

namespace AngelProtocol
{
    public class WeightCommonObject
    {
        private static readonly byte[] ENQ = new byte[] { 0x05 };
        private static readonly byte[] DC1 = new byte[] { 0x11 };
        private static readonly byte[] STX = new byte[] { 0x05 };
        private const string EXT = "\u0003";
        private const string EOT = "\u0004";
        private const string ACK = "\u0006";

        private string _response { get; set; }
        private bool _endReading { get; set; }
        private bool _ackIsRecived { get; set; }
        private ComPortSettings _comPortSettings { get; set; }
        public string Message { get; protected set; }

        public WeightCommonObject(ComPortSettings comPortSettings)
        {
            _comPortSettings = comPortSettings;
        }

        public decimal GetWeihgtBasicProtocol()
        {
            _response = "";
            _endReading = false;
            SerialPort serialPort = new SerialPort
            {
                BaudRate = _comPortSettings.BaudRate,
                PortName = _comPortSettings.PortName,
                StopBits = (StopBits)_comPortSettings.StopBits,
                DataBits = _comPortSettings.DataBits,
                Parity = (Parity)_comPortSettings.Parity,
                Encoding = Encoding.ASCII,
                Handshake = Handshake.None
            };

            serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceivedBasicProtocol);
            try
            {
                serialPort.Open();
                serialPort.Write(STX, 0, 1);
                var start = DateTime.Now.Ticks;
                while (!_endReading)
                {
                    var end = DateTime.Now.Ticks;
                    if (end > (start + 10000000))
                    {
                        serialPort.Close();
                        break;
                    }
                }
                char[] charArray = _response.ToCharArray();
                Array.Reverse(charArray);
                return ParseWeightResultBasicProtocol(new string(charArray));
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return 0;
            }
        }

        public decimal GetWeihgtAngelProtocol()
        {
            _response = "";
            _endReading = false;
            SerialPort serialPort = new SerialPort
            {
                BaudRate = _comPortSettings.BaudRate,
                PortName = _comPortSettings.PortName,
                StopBits = (StopBits)_comPortSettings.StopBits,
                DataBits = _comPortSettings.DataBits,
                Parity = (Parity)_comPortSettings.Parity,
                Encoding = Encoding.ASCII,
                Handshake = Handshake.None
            };

            serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceivedAngelProtocol);
            try
            {
                serialPort.Open();
                serialPort.Write(ENQ, 0, 1);
                var start = DateTime.Now.Ticks;
                while (!_ackIsRecived)
                {
                    var end = DateTime.Now.Ticks;
                    if (end > (start + 15000000)) //wait 1.5 sec for ACK
                    {
                        serialPort.Close();
                        return 0;
                    }
                }
                serialPort.Write(DC1, 0, 1);
                while (!_endReading)
                {
                    var end = DateTime.Now.Ticks;
                    if (end > (start + 10000000)) //wait 1 sec for DC1 response
                    {
                        serialPort.Close();
                        return 0;
                    }
                }
                char[] charArray = _response.ToCharArray();
                return ParseWeightResultAngelProtocol(new string(charArray));
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return 0;
            }
        }

        private void SerialPort_DataReceivedBasicProtocol(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            _response += sp.ReadExisting();
            if (_response.Contains(EXT))
            {
                sp.Close();
                _endReading = true;
            }
        }

        private void SerialPort_DataReceivedAngelProtocol(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            _response += sp.ReadExisting();
            if (_response.Contains(ACK))
            {
                _ackIsRecived = true;
                _response = "";
            }

            if (_response.Contains(EOT))
            {
                sp.Close();
                _endReading = true;
            }
        }

        private decimal ParseWeightResultAngelProtocol(string input)
        {
            try
            {
                var weightResult = input.Substring(4, 6).Replace(".", ",");
                return decimal.Parse(weightResult);
            }
            catch
            {
                return 0;
            }
        }

        private decimal ParseWeightResultBasicProtocol(string input)
        {
            try
            {
                var decimalPoint = double.Parse(input[2].ToString());
                var intValue = int.Parse(input.Substring(3, input.Length - 4));
                return (decimal)(intValue / (Math.Pow(10, decimalPoint)));
            }
            catch
            {
                return 0;
            }
        }
    }
}
