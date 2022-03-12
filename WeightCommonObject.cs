using System;
using System.IO.Ports;
using System.Text;

namespace AngelProtocol
{
    public class WeightCommonObject
    {
        private static readonly byte[] ENQ = new byte[] { 0x05 };
        private static readonly byte[] DC1 = new byte[] { 0x11 };
        //private static readonly byte[] DC2 = new byte[] { 0x12 };
        private static readonly byte[] STX = new byte[] { 0x05 };
        private const string ACK = "\u0006";
        private const string EOT = "\u0004";
        private string _response { get; set; }
        private bool _endReading { get; set; }
        private bool _ackIsRecived { get; set; }
        private ComPortSettings _comPortSettings { get; set; }
        public string Message { get; protected set; }

        public WeightCommonObject(ComPortSettings comPortSettings)
        {
            _comPortSettings = comPortSettings;
        }

        public decimal GetWeihgt()
        {
            _response = "";
            _endReading = false;
            decimal parsedResponse = 0M;
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

            serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
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
                parsedResponse = ParseResponse(new string(charArray));
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }

            return parsedResponse;
        }

        public decimal GetWeihgtApProt()
        {
            _response = "";
            _endReading = false;
            decimal parsedResponse = 0M;
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

            serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_ApProtDataReceived);
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
                        return parsedResponse;
                    }
                }
                serialPort.Write(DC1, 0, 1);
                while (!_endReading)
                {
                    var end = DateTime.Now.Ticks;
                    if (end > (start + 10000000)) //wait 1 sec for DC1 response
                    {
                        serialPort.Close();
                        return parsedResponse;
                    }
                }
                char[] charArray = _response.ToCharArray();
                parsedResponse = ParseResponseApProt(new string(charArray));
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }

            return parsedResponse;
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            _response += sp.ReadExisting();
            if (_response.Contains("\u0003"))
            {
                sp.Close();
                _endReading = true;
            }
        }

        private void SerialPort_ApProtDataReceived(object sender, SerialDataReceivedEventArgs e)
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

        private decimal ParseResponseApProt(string input)
        {
            decimal response = 0;
            try
            {
                input = input.Substring(4, 6).Replace(".", ",");
                response = decimal.Parse(input);
            }
            catch
            {
                //ignore
            }
            return response;
        }

        private decimal ParseResponse(string input)
        {
            decimal response = 0;
            try
            {
                var decimalPoint = double.Parse(input[2].ToString());
                var intValue = int.Parse(input.Substring(3, input.Length - 4));
                response = (decimal)(intValue / (Math.Pow(10, decimalPoint)));
            }
            catch
            {
                //ignore
            }
            return response;
        }
    }
}
