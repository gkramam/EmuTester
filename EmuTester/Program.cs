using System;
using System.Collections.Concurrent;
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
        static ConnectionWoker robotLoop, preAlignLoop;

        static void Main(string[] args)
        {
            
            Console.WriteLine("Emulator-Tester Started");

            robotLoop = new ConnectionWoker(50100);
            robotLoop.Start();

            preAlignLoop = new ConnectionWoker(50101);
            preAlignLoop.Start();

            ConsoleKey key = ConsoleKey.Escape;
            do
            {
                //RunScriptWithSeqNum();
                //RunScriptWithoutSeqNum();
                RunScriptWithoutCheckSum();
                Console.WriteLine("Press A to Repeat or ENTER to quit");
                key = Console.ReadKey().Key;
            } while (key == ConsoleKey.A);

            robotLoop.Stop();
            preAlignLoop.Stop();
        }
        static void RunScriptWithoutSeqNum()
        {
            Script robotScript = new Script(robotLoop);
            Script preAlignerScript = new Script(preAlignLoop);

            Console.WriteLine("!---    Robot Initialization ----!");
            robotScript.Execute("$,1,INIT,1,1,G,");

            Console.WriteLine("\n!----    Pre-Aligner Initialization    -----!");
            preAlignerScript.Execute("$,2,INIT,1,1,G,");

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
        }

        static void RunScriptWithSeqNum()
        {
            Script robotScript = new Script(robotLoop);
            Script preAlignerScript = new Script(preAlignLoop);

            Console.WriteLine("!---    Robot Initialization ----!");
            robotScript.Execute("$,1,01,INIT,1,1,G,");

            Console.WriteLine("\n!---    Robot Get ----!");
            robotScript.Execute("$,1,02,MTRS,G,C02,05,L,1,G1,");

            Console.WriteLine("\n!---    Robot Put ----!");
            robotScript.Execute("$,1,03,MTRS,P,P01,00,R,2,P4,00090000,");

            Console.WriteLine("\n!---    Robot Get ----!");
            robotScript.Execute("$,1,04,MTRS,G,S01,00,L,2,G4,00000250,-0000300,00000000,00000000,");


            Console.WriteLine("\n!---    Robot Get ----!");
            robotScript.Execute("$,1,05,MTRS,G,P01,00,L,1,G1,");

            Console.WriteLine("\n!---    Robot Motion Transfer Point ----!");
            robotScript.Execute("$,1,06,MPNT,G3,");

            Console.WriteLine("\n!---    Robot Continued Transfer ----!");
            robotScript.Execute("$,1,07,MCTR,P,P01,00,L,1,P4,00090000,");

            Console.WriteLine("\n!---    Robot Move To Specified Position ----!");
            robotScript.Execute("$,1,08,MTCH,C04,01,R,1,R,00000000,00000000,00000000,");

            Console.WriteLine("\n!---    Robot Move To Specified Position ----!");
            robotScript.Execute("$,1,09,MTCH,C01,01,L,1,B,");

            Console.WriteLine("\n!---    Robot Move Axis To Specified Position ----!");
            robotScript.Execute("$,1,10,MABS,H,1,C,-0000300,");

            Console.WriteLine("\n!---    Robot Move Axis To Specified Relative Position ----!");
            robotScript.Execute("$,1,11,MREL,H,1,C,-0000300,");

            Console.WriteLine("\n!---    Robot Wafer Map ----!");
            robotScript.Execute("$,1,12,MMAP,C01,03,A,0,");

            Console.WriteLine("\n!---    Robot Mapping Calibration ----!");
            robotScript.Execute("$,1,13,MMCA,C01,L,0,");


            Console.WriteLine("\n\n ###########    PRE ALIGNER #############");

            Console.WriteLine("\n!----    Pre-Aligner Initialization    -----!");
            preAlignerScript.Execute("$,2,01,INIT,1,1,G,");

            Console.WriteLine("\n!----    Pre-Aligner Wafer Align    -----!");
            preAlignerScript.Execute("$,2,02,MALN,0,00000010,");

            Console.WriteLine("\n!----    Pre-Aligner Alignment calibration    -----!");
            preAlignerScript.Execute("$,2,03,MACA,0,");
        }

        static void RunScriptWithoutCheckSum()
        {
            Script robotScript = new Script(robotLoop);
            Script preAlignerScript = new Script(preAlignLoop);

            Console.WriteLine("!---    Robot Initialization ----!");
            robotScript.ExecuteWithoutCheckSum("$,1,INIT,1,1,G,");

            Console.WriteLine("\n!----    Pre-Aligner Initialization    -----!");
            preAlignerScript.ExecuteWithoutCheckSum("$,2,INIT,1,1,G,");

            Console.WriteLine("\n!---    Robot Get ----!");
            robotScript.ExecuteWithoutCheckSum("$,1,MTRS,G,C02,05,L,1,G1,");

            Console.WriteLine("\n!---    Robot Put ----!");
            robotScript.ExecuteWithoutCheckSum("$,1,MTRS,P,P01,00,R,2,P4,00090000,");

            Console.WriteLine("\n!---    Robot Get ----!");
            robotScript.ExecuteWithoutCheckSum("$,1,MTRS,G,S01,00,L,2,G4,00000250,-0000300,00000000,00000000,");


            Console.WriteLine("\n!---    Robot Get ----!");
            robotScript.ExecuteWithoutCheckSum("$,1,MTRS,G,P01,00,L,1,G1,");

            Console.WriteLine("\n!---    Robot Motion Transfer Point ----!");
            robotScript.ExecuteWithoutCheckSum("$,1,MPNT,G3,");

            Console.WriteLine("\n!---    Robot Continued Transfer ----!");
            robotScript.ExecuteWithoutCheckSum("$,1,MCTR,P,P01,00,L,1,P4,00090000,");

            Console.WriteLine("\n!---    Robot Move To Specified Position ----!");
            robotScript.ExecuteWithoutCheckSum("$,1,MTCH,C04,01,R,1,R,00000000,00000000,00000000,");

            Console.WriteLine("\n!---    Robot Move To Specified Position ----!");
            robotScript.ExecuteWithoutCheckSum("$,1,MTCH,C01,01,L,1,B,");

            Console.WriteLine("\n!---    Robot Move Axis To Specified Position ----!");
            robotScript.ExecuteWithoutCheckSum("$,1,MABS,H,1,C,-0000300,");

            Console.WriteLine("\n!---    Robot Move Axis To Specified Relative Position ----!");
            robotScript.ExecuteWithoutCheckSum("$,1,MREL,H,1,C,-0000300,");

            Console.WriteLine("\n!---    Robot Wafer Map ----!");
            robotScript.ExecuteWithoutCheckSum("$,1,MMAP,C01,03,A,0,");

            Console.WriteLine("\n!---    Robot Mapping Calibration ----!");
            robotScript.ExecuteWithoutCheckSum("$,1,MMCA,C01,L,0,");


            Console.WriteLine("\n\n ###########    PRE ALIGNER #############");

            Console.WriteLine("\n!----    Pre-Aligner Initialization    -----!");
            preAlignerScript.ExecuteWithoutCheckSum("$,2,INIT,1,1,G,");

            Console.WriteLine("\n!----    Pre-Aligner Wafer Align    -----!");
            preAlignerScript.ExecuteWithoutCheckSum("$,2,MALN,0,00000010,");

            Console.WriteLine("\n!----    Pre-Aligner Alignment calibration    -----!");
            preAlignerScript.ExecuteWithoutCheckSum("$,2,MACA,0,");
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

