using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EmuTester
{
    class Program
    {
        static void Main(string[] args)
        {
            CalcluateCheckSum(",1,INIT,1,1,G,");
            return;
            var msg = ASCIIEncoding.ASCII.GetBytes("Hello Host");
            byte[] respnose = new byte[1024];
            TcpClient client = new TcpClient();
            client.Connect("DESKTOP-REVDIF4", 50001);
            Console.WriteLine("Control software connected");

            Stream s = client.GetStream();
            StreamReader sr = new StreamReader(s, Encoding.ASCII);
            StreamWriter sw = new StreamWriter(s, Encoding.ASCII);
            sw.AutoFlush = true;

            //Random rnd = new Random();

            //for (int j = 0; j < 10; j++)
            //{
            //    for (int i = 0; i < 10; i++)
            //    {
            //        char randomChar = (char)rnd.Next('a', 'z');
            //        sw.Write(randomChar);
            //    }
            //    sw.Flush();
            //    Thread.Sleep(2000);
            //}

            StringBuilder initCommandStr = new StringBuilder(string.Empty);
            initCommandStr.Append('$');
            initCommandStr.Append('1');
            initCommandStr.Append("01");
            initCommandStr.Append('0');
            initCommandStr.Append('0');
            initCommandStr.Append('G');
            initCommandStr.Append('0');
            initCommandStr.Append('\r');

            sw.WriteLine(initCommandStr);

            var response = sr.ReadLine();
            Console.WriteLine($"Received Init Response : {response}");
            Console.WriteLine("Press return key to exit");
            Console.ReadLine();
        }

        static void CalcluateCheckSum(string unicodeMsg)
        {
            unicodeMsg = ",1,INIT,1,1,G,";
            byte[] asciiBytes = ASCIIEncoding.ASCII.GetBytes(unicodeMsg);
            //var hexString = BitConverter.ToString(asciiBytes);
            int count = 0;
            for (int i = 0; i < asciiBytes.Length; i++)
            {
                count += asciiBytes[i];
            }

            var hexsumBytes = BitConverter.GetBytes(count);
            var hexsumString = BitConverter.ToString(hexsumBytes, 0, 1);
            unicodeMsg += (hexsumString + '\r');

            byte uS = 0b00000010;
            byte ss = 0b00000100;
            var sts1 = (uS | ss);

            //var hexsts1 = BitConverter.ToString(new byte[] { (byte)sts1 });

            //var asciiSts1 = ASCIIEncoding.ASCII.GetBytes(hexsts1.ToCharArray(),1,1);

            //var hexAsciiSts1 = BitConverter.ToString(asciiSts1, 0, 1);

            Status1 sts = Status1.ServoOff | Status1.ErrorOccured;
            var stsStr = sts.ToString("X");
            var hexsts1 = BitConverter.ToString(new byte[] { (byte)sts });//Getting the Hex Value
            var asciiSts1 = ASCIIEncoding.ASCII.GetBytes(hexsts1.ToCharArray(), 1, 1);


            //var temp = ASCIIEncoding.ASCII.GetString(new byte[] {sts});


        }
    }

    [Flags]
    public enum Status1 : short
    {
        None = 0x0,
        UnitReady = 0x2,//1 ready, 0 busy
        ServoOff = 0x4,
        ErrorOccured = 0x8
    }
}

