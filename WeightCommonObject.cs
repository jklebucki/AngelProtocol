using System;
using System.IO.Ports;
using System.Text;

namespace AngelProtocol
{
    public class WeightCommonObject
    {
        private string response { get; set; }
        private bool endReading { get; set; }
        private bool ackIsRecived { get; set; }
        private ComPortSettings comPortSettings { get; set; }
        public string Message { get; protected set; }

        public WeightCommonObject(ComPortSettings comPortSettings)
        {
            this.comPortSettings = comPortSettings;
        }

        public decimal GetWeihgt()
        {
            response = "";
            endReading = false;
            decimal parsedResponse = 0M;
            SerialPort serialPort = new SerialPort
            {
                BaudRate = comPortSettings.BaudRate,
                PortName = comPortSettings.PortName,
                StopBits = (StopBits)comPortSettings.StopBits,
                DataBits = comPortSettings.DataBits,
                Parity = (Parity)comPortSettings.Parity,
                Encoding = Encoding.ASCII,
                Handshake = Handshake.None
            };

            serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
            try
            {
                serialPort.Open();
                byte[] STX = new byte[] { 0x05 };
                serialPort.Write(STX, 0, 1);
                var start = DateTime.Now.Ticks;
                while (!endReading)
                {
                    var end = DateTime.Now.Ticks;
                    if (end > (start + 10000000))
                    {
                        serialPort.Close();
                        break;
                    }
                }
                char[] charArray = response.ToCharArray();
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
            response = "";
            endReading = false;
            decimal parsedResponse = 0M;
            SerialPort serialPort = new SerialPort
            {
                BaudRate = comPortSettings.BaudRate,
                PortName = comPortSettings.PortName,
                StopBits = (StopBits)comPortSettings.StopBits,
                DataBits = comPortSettings.DataBits,
                Parity = (Parity)comPortSettings.Parity,
                Encoding = Encoding.ASCII,
                Handshake = Handshake.None
            };

            serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_ApProtDataReceived);
            try
            {
                serialPort.Open();
                byte[] ENQ = new byte[] { 0x05 };
                byte[] DC1 = new byte[] { 0x11 };
                byte[] DC2 = new byte[] { 0x12 };

                serialPort.Write(ENQ, 0, 1);
                var start = DateTime.Now.Ticks;
                while (!ackIsRecived)
                {
                    var end = DateTime.Now.Ticks;
                    if (end > (start + 15000000)) //wait 1.5 sec for ACK
                    {
                        serialPort.Close();
                        return parsedResponse;
                    }
                }
                serialPort.Write(DC1, 0, 1);
                while (!endReading)
                {
                    var end = DateTime.Now.Ticks;
                    if (end > (start + 10000000)) //wait 1 sec for DC1 response
                    {
                        serialPort.Close();
                        return parsedResponse;
                    }
                }
                char[] charArray = response.ToCharArray();
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
            response += sp.ReadExisting();
            if (response.Contains("\u0003"))
            {
                sp.Close();
                endReading = true;
            }
        }

        private void SerialPort_ApProtDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var ACK = "\u0006";
            SerialPort sp = (SerialPort)sender;
            response += sp.ReadExisting();
            if (response.Contains(ACK))
            {
                ackIsRecived = true;
                response = "";
            }
            var EOT = "\u0004";
            if (response.Contains(EOT))
            {
                sp.Close();
                endReading = true;
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
