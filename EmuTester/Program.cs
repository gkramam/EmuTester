using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmuTester
{
    class Program
    {
        static void Main(string[] args)
        {
            //CalcluateCheckSum(",1,INIT,1,1,G,");
            //Parse();
            //return;
            var msg = ASCIIEncoding.ASCII.GetBytes("Hello Host");
            byte[] respnose = new byte[1024];
            TcpClient client = new TcpClient();
            //client.ReceiveTimeout = 600000;
            client.Connect("localhost", 50100);
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

            string cmdStr1 = "$,1,INIT,1,1,G,16\r";
            var key = ConsoleKey.Enter;
            var chars1 = cmdStr1.ToString().ToCharArray();
            //sw.Write(cmdStr);
            string cmdStr2 = "$,1,INIT,1,1,A,10\r";
            string cmdStr3 = "$,1,INIT,1,1,N,1D\r";

            var chars2 = cmdStr2.ToString().ToCharArray();
            var chars3 = cmdStr3.ToString().ToCharArray();

            foreach (char c in chars1)
            {
                sw.Write(c);
                //sw.Flush();
                Thread.Sleep(100);
            }
            //sw.WriteLine(cmdStr1);
            var response = sr.ReadLine();
            while (string.IsNullOrEmpty(response))
            {
                response = sr.ReadLine();
            }
            Console.WriteLine($"Received Init Response : {response}");

            foreach (char c in chars2)
            {
                sw.Write(c);
                //sw.Flush();
                Thread.Sleep(100);
            }
            //sw.WriteLine(cmdStr2);
            var response2 = sr.ReadLine();

            while(string.IsNullOrEmpty(response2))
            {
                response2 = sr.ReadLine();
            }
            Console.WriteLine($"Received Init Response : {response2}");

            foreach (char c in chars3)
            {
                sw.Write(c);
                //sw.Flush();
                Thread.Sleep(100);
            }
            //sw.WriteLine(cmdStr3);
            var response3 = sr.ReadLine();

            while (string.IsNullOrEmpty(response3))
            {
                response3 = sr.ReadLine();
            }
            Console.WriteLine($"Received Init Response : {response3}");

            Console.WriteLine("Press return to Send again or other to quit");
            Console.ReadLine();
        }

        static void Parse()
        {
            string message = "$,1,INIT,1,1,G,16\r";
            var strippeedCmd = message.Substring(2, message.Length - 5);
            var fields = strippeedCmd.Split(',');
            int unitNumber = Convert.ToInt32(fields[0]);
            string commandName = fields[1];
        }
        static void CalcluateCheckSum(string unicodeMsg)
        {
            unicodeMsg = ",1,INIT,1,1,N,";
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

