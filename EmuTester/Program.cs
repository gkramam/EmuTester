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
            Console.WriteLine("Emulator-Tester Started");
            //TcpClient client = new TcpClient();
            //client.Connect("localhost", 50100);

            //Stream s = client.GetStream();
            //StreamReader sr = new StreamReader(s, Encoding.ASCII);
            //StreamWriter sw = new StreamWriter(s, Encoding.ASCII);
            //sw.AutoFlush = true;

            ConnectionWoker robotLoop = new ConnectionWoker(50100);
            robotLoop.Start();

            ConnectionWoker preAlignLoop = new ConnectionWoker(50101);
            preAlignLoop.Start();

            Script robotScript = new Script(robotLoop);
            Script preAlignerScript = new Script(preAlignLoop);

            Console.WriteLine("!---    Robot Initialization ----!");
            robotScript.Execute("$,1,INIT,1,1,G,");

            Console.WriteLine("\n!---    Robot Get ----!");
            robotScript.Execute("$,1,MTRS,G,C02,05,L,1,G1,");
            
            Console.WriteLine("\n!---    Robot Put ----!");
            robotScript.Execute("$,1,MTRS,P,P01,00,R,2,P4,00090000,");

            Console.WriteLine("\n!---    Robot Get ----!");
            robotScript.Execute("$,1,MTRS,G,S01,00,L,2,G4,00000250,-0000300,00000000,00000000,");

            
            Console.WriteLine("\n!---    Robot Get ----!");
            robotScript.Execute("$,1,MTRS,G,P01,00,L,1,G1,");

            Console.WriteLine("\n!---    Robot Motion Transfer Point ----!");
            robotScript.Execute("$,1,MPNT,G3,");

            Console.WriteLine("\n!---    Robot Continued Transfer ----!");
            robotScript.Execute("$,1,MCTR,P,P01,00,L,1,P4,00090000,");

            Console.WriteLine("\n!---    Robot Move To Specified Position ----!");
            robotScript.Execute("$,1,MTCH,C04,01,R,1,R,00000000,00000000,00000000,");

            Console.WriteLine("\n!---    Robot Move To Specified Position ----!");
            robotScript.Execute("$,1,MTCH,C01,01,L,1,B,");

            Console.WriteLine("\n!---    Robot Move Axis To Specified Position ----!");
            robotScript.Execute("$,1,MABS,H,1,C,-0000300,");

            Console.WriteLine("\n!---    Robot Move Axis To Specified Relative Position ----!");
            robotScript.Execute("$,1,MREL,H,1,C,-0000300,");

            Console.WriteLine("\n!---    Robot Wafer Map ----!");
            robotScript.Execute("$,1,MMAP,C01,03,A,0,");

            Console.WriteLine("\n!---    Robot Mapping Calibration ----!");
            robotScript.Execute("$,1,MMCA,C01,L,0,");


            Console.WriteLine("\n\n ###########    PRE ALIGNER #############");

            Console.WriteLine("\n!----    Pre-Aligner Initialization    -----!");
            preAlignerScript.Execute("$,2,INIT,1,1,G,");

            Console.WriteLine("\n!----    Pre-Aligner Wafer Align    -----!");
            preAlignerScript.Execute("$,2,MALN,0,00000010,");

            Console.WriteLine("\n!----    Pre-Aligner Alignment calibration    -----!");
            preAlignerScript.Execute("$,2,MACA,0,");


            //var chars1 = cmdStr1.ToString().ToCharArray();
            //var chars2 = cmdStr2.ToString().ToCharArray();
            //var chars3 = cmdStr3.ToString().ToCharArray();

            //foreach (char c in chars1)
            //{
            //    sw.Write(c);
            //    //sw.Flush();
            //    Thread.Sleep(100);
            //}
            ////sw.WriteLine(cmdStr1);
            //var response = sr.ReadLine();
            //while (string.IsNullOrEmpty(response))
            //{
            //    response = sr.ReadLine();
            //}
            //Console.WriteLine($"Received Init Response : {response}");

            //foreach (char c in chars2)
            //{
            //    sw.Write(c);
            //    //sw.Flush();
            //    Thread.Sleep(100);
            //}
            ////sw.WriteLine(cmdStr2);
            //var response2 = sr.ReadLine();

            //while(string.IsNullOrEmpty(response2))
            //{
            //    response2 = sr.ReadLine();
            //}
            //Console.WriteLine($"Received Init Response : {response2}");

            //foreach (char c in chars3)
            //{
            //    sw.Write(c);
            //    //sw.Flush();
            //    Thread.Sleep(100);
            //}
            ////sw.WriteLine(cmdStr3);
            //var response3 = sr.ReadLine();

            //while (string.IsNullOrEmpty(response3))
            //{
            //    response3 = sr.ReadLine();
            //}
            //Console.WriteLine($"Received Init Response : {response3}");

            //Console.WriteLine("Press return to Send again or other to quit");
            Console.ReadLine();
        }

        static void Parse()
        {
            TimeSpan span = TimeSpan.FromMilliseconds(3);
            var str = span.ToString("ffffff");
            string message = "$,1,INIT,1,1,G,16\r";
            var strippeedCmd = message.Substring(2, message.Length - 5);
            var fields = strippeedCmd.Split(',');
            int unitNumber = Convert.ToInt32(fields[0]);
            string commandName = fields[1];
        }
        static string CalcluateCheckSum(string unicodeMsg)
        {
            //unicodeMsg = ",1,INIT,1,1,N,";
            //unicodeMsg = ",1,ACKN,";

            byte[] asciiBytes = ASCIIEncoding.ASCII.GetBytes(unicodeMsg);
            //var hexString = BitConverter.ToString(asciiBytes);
            int count = 0;
            for (int i = 0; i < asciiBytes.Length; i++)
            {
                count += asciiBytes[i];
            }

            var hexsumBytes = BitConverter.GetBytes(count);
            var hexsumString = BitConverter.ToString(hexsumBytes, 0, 1);
            //unicodeMsg += (hexsumString + '\r');
            return hexsumString;
        }
    }

    public static class CheckSum
    {
        public static string Compute(string unicodeMsg)
        {
            try
            {
                //unicodeMsg = ",1,INIT,1,1,G,";
                             //",1,INIT,1,1,G,")
                byte[] asciiBytes = ASCIIEncoding.ASCII.GetBytes(unicodeMsg);
                //var hexString = BitConverter.ToString(asciiBytes);
                int count = 0;
                for (int i = 0; i < asciiBytes.Length; i++)
                {
                    count += asciiBytes[i];
                }

                var hexsumBytes = BitConverter.GetBytes(count);
                var hexsumString = BitConverter.ToString(hexsumBytes, 0, 1);
                return hexsumString;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
                throw;
            }
        }

        public static bool Valid(string message, string rcvdCheckSum)
        {
            var computed = Compute(message.ToString());

            if (computed.Equals(rcvdCheckSum))
                return true;
            else
            {
                Console.WriteLine($"computed checksum: {computed} \t rcvd: {rcvdCheckSum}");
                return false;
            }
        }
    }
}

