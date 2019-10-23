using System;
using System.Text;
using System.IO.Ports;

namespace AngelProtocol
{
    public class WeightCommonInterface
    {
        private string response { get; set; }
        private bool endReading { get; set; }
        private ComPortSettings comPortSettings { get; set; }
        public string Message { get; protected set; }

        public WeightCommonInterface(ComPortSettings comPortSettings)
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
                Encoding = Encoding.UTF8,
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
