using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace EmuTester
{
    class Program
    {
        public static BlockingCollection<string> ConsoleQ = new BlockingCollection<string>();
        static ConnectionWoker robotLoop, preAlignLoop;
        static List<LoadPortConnectionWorker> loadPortLoop;

        static void Main(string[] args)
        {
            //ProcessXML();
            //return;

            Console.WriteLine("Emulator-Tester Started");

            Task.Run(() =>
            {
                foreach (var s in ConsoleQ.GetConsumingEnumerable())
                {
                    Console.WriteLine(s);
                }
            });

            //robotLoop = new ConnectionWoker(50100);
            //robotLoop.Start();

            //preAlignLoop = new ConnectionWoker(50101);
            //preAlignLoop.Start();
            loadPortLoop = new List<LoadPortConnectionWorker>();
            loadPortLoop.Add(new LoadPortConnectionWorker(5001));
            loadPortLoop.Add(new LoadPortConnectionWorker(5002));
            loadPortLoop.Add(new LoadPortConnectionWorker(5003));
            loadPortLoop.Add(new LoadPortConnectionWorker(5004));
            loadPortLoop.ForEach(l=>l.Start());

            ConsoleKey key = ConsoleKey.Escape;
            do
            {
                //RunScripts(false,false);
                //RunScripts(false, true);
                //RunScripts(true, false);
                //RunScripts(true, true);
                RunLoadPortScript();
                //RunScriptWithSeqNumAndWithChecksumWithReferenceMessages();
                Console.WriteLine("Press A to Repeat or ENTER to quit");
                key = Console.ReadKey().Key;
            } while (key == ConsoleKey.A);
            //} while (true);

            //robotLoop.Stop();
            //preAlignLoop.Stop();
            loadPortLoop.ForEach(l => l.Stop());
        }

        static void RunLoadPortScript()
        {
            List<LoadPortScript> lpScripts = new List<LoadPortScript>();
            loadPortLoop.ForEach(l => lpScripts.Add(new LoadPortScript(l)));

            //lpScript.Execute("s00SET:INITL;\r\n");
            //lpScript.Execute("s00MOV:ORGSH;\r\n");
            //lpScript.Execute("s00MOV:CLOAD;\r\n");
            //lpScript.Execute("s00MOV:CLDMP;\r\n");
            //lpScript.Execute("s00MOV:CLDDK;\r\n");
            //lpScript.Execute("s00MOV:CLDYD;\r\n");
            //lpScript.Execute("s00MOV:CLDOP;\r\n");
            //lpScript.Execute("s00MOV:CLMPO;\r\n");
            //lpScript.Execute("s00MOV:MAPDO;\r\n");
            //lpScript.Execute("s00MOV:REMAP;\r\n");

            ////unloadCommands
            //lpScript.Execute("s00MOV:CULOD;\r\n");
            //lpScript.Execute("s00MOV:CULDK;\r\n");
            //lpScript.Execute("s00MOV:CUDCL;\r\n");
            //lpScript.Execute("s00MOV:CUDNC;\r\n");
            //lpScript.Execute("s00MOV:CULYD;\r\n");
            //lpScript.Execute("s00MOV:CULFC;\r\n");
            //lpScript.Execute("s00MOV:CUDMP;\r\n");

            List<string> commands = new List<string>() {
            "s00SET:INITL;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:ORGSH;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:CLOAD;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:CLDMP;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:CLDDK;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:CLDYD;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:CLDOP;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:CLMPO;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:MAPDO;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:REMAP;\r\n",
            "s00GET:MAPRD;\r\n",
            "s00GET:MAPDT;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:CULOD;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:CULDK;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:CUDCL;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:CUDNC;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:CULYD;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:CULFC;\r\n",
            "s00GET:STATE;\r\n",
            "s00MOV:CUDMP;\r\n",
            "s00GET:STATE;\r\n"};

            for(int i=0; i<commands.Count;i=i+2)
            {
                List<Task> round = new List<Task>();

                lpScripts.ForEach(l => {
                    round.Add(
                                Task.Run(() =>
                                {
                                    l.Execute(commands[i], i == 0);
                                    l.Execute(commands[i + 1]);
                                }));
                });

                Task.WaitAll(round.ToArray());
                
                Console.WriteLine("Press Enter to Execute Next Command");
                Console.ReadLine();

            }

        }
        static void ProcessXML()
        {
            XmlDocument doc = new XmlDocument();
            doc.Load("Environment.xml");
            Environment env = new Environment(doc.SelectSingleNode("/Environment"));
        }

        public class Environment
        {
            public Manipulator Manipulator { get; set; }
            public Prealigner Prealigner { get; set; }
            public Environment(XmlNode envNode)
            {
                if(envNode == null)
                {
                    throw new ApplicationException("Invalid Environment xml file. No Environment node found");
                }

                Manipulator = new Manipulator(envNode.SelectSingleNode("//Device[@name='Manipulator']"));
                Prealigner = new Prealigner(envNode.SelectSingleNode("//Device[@name='PreAligner']"));
            }
        }

        public class ManipulatorPosition
        {
            public double RotationAxis { get; set; }
            public double ExtensionAxis { get; set; }
            public double WristAxis1 { get; set; }
            public double WristAxis2 { get; set; }
            public double ElevationAxis { get; set; }

            public ManipulatorPosition(XmlNode positionNode)
            {

                if (positionNode == null)
                {
                    throw new ApplicationException("Invalid Environment xml file. No position node found");
                }

                RotationAxis = double.Parse(positionNode["RotationAxis"].InnerText) * 0.001;
                ExtensionAxis = double.Parse(positionNode["ExtensionAxis"].InnerText) * 0.001;
                WristAxis1 = double.Parse(positionNode["WristAxis1"].InnerText) * 0.001;
                WristAxis2 = double.Parse(positionNode["WristAxis2"].InnerText) * 0.001;
                ElevationAxis = double.Parse(positionNode["ElevationAxis"].InnerText) * 0.001;
            }
        }

        public enum StationType
        {
            Casette,
            Transfer,
            PreAligner
        }

        public enum SlotStatus
        {
            Empty,
            Present,
            Protruded,
            DoubleInsertion,
            Inclined
        }

        public class Slot
        {
            public int ID { get; set; }
            public SlotStatus Status { get; set; }
            public Slot(XmlNode slotNode)
            {
                if (slotNode == null)
                {
                    throw new ApplicationException("Invalid Environment xml file. No slot node found");
                }

                ID = Convert.ToInt32(slotNode.Attributes["id"].Value);
                Status = (SlotStatus)Enum.Parse(typeof(SlotStatus), slotNode.Attributes["status"].Value);
            }
        }

        public class Threshold
        {
            public double Value { get; set; }

            public MappingCalibrationThresholdType Type { get; set; }

            public Threshold(XmlNode tNode)
            {
                if (tNode == null)
                {
                    throw new ApplicationException("Invalid Environment xml file. No threshold node found");
                }

                Type = (MappingCalibrationThresholdType)Enum.Parse(typeof(MappingCalibrationThresholdType), tNode.Name);
                Value = double.Parse(tNode.InnerText) * 0.001;
            }

            public override string ToString()
            {
                return ((int)Value / 0.001).ToString("D8");
            }

        }

        public enum MappingCalibrationThresholdType
        {
            DoubleInsertion,
            SlantingInsertion1,
            SlantingInsertion2
        }

        public class Station
        {
            public StationType Type { get; set; }
            public string ID { get; set; }

            public List<Slot> Slots { get; set; }

            public ManipulatorPosition LowestRegisteredPosition { get; set; }
            public ManipulatorPosition HighestRegisteredPosition { get; set; }
            public ManipulatorPosition RegisteredG4Position { get; set; }
            public ManipulatorPosition RegisteredP4Position { get; set; }

            public List<Threshold> Thresholds { get; set; }

            public double WaferWidth { get; set; }

            public double LowestSlotPosition { get; set; }
            public double HighestSlotPosition { get; set; }
            public Station(XmlNode stationNode)
            {
                if (stationNode == null)
                {
                    throw new ApplicationException("Invalid Environment xml file. No station node found");
                }

                Type = (StationType) Enum.Parse(typeof(StationType), stationNode.Attributes["type"].Value);
                ID = stationNode.Attributes["id"].Value;
                
                Slots = new List<Slot>();
                foreach(XmlNode slotNode in stationNode.SelectNodes("Slot"))
                {
                    Slots.Add(new Slot(slotNode));
                }

                LowestRegisteredPosition = new ManipulatorPosition(stationNode.SelectSingleNode("Positions/Position[@key='Lowest']"));
                
                if(Type == StationType.Casette)
                    HighestRegisteredPosition = new ManipulatorPosition(stationNode.SelectSingleNode("Positions//Position[@key='Highest']"));
                
                RegisteredG4Position = new ManipulatorPosition(stationNode.SelectSingleNode("Positions//Position[@key='G4']"));
                RegisteredP4Position = new ManipulatorPosition(stationNode.SelectSingleNode("Positions//Position[@key='P4']"));

                Thresholds = new List<Threshold>();
                if (Type == StationType.Casette)
                {
                    foreach (XmlNode t in stationNode.SelectSingleNode("Thresholds").ChildNodes)
                    {
                        Thresholds.Add(new Threshold(t));
                    }
                }

                WaferWidth = double.Parse(stationNode["WaferWidth"].InnerText) * 0.001;
            }
        }
        public class Manipulator
        {
            public ManipulatorPosition HomePosition { get; set; }

            public List<Station> Stations { get; set; }
            public Manipulator(XmlNode manipulatorNode)
            {
                if (manipulatorNode == null)
                {
                    throw new ApplicationException("Invalid Environment xml file. No Manipulator node found");
                }

                HomePosition = new ManipulatorPosition(manipulatorNode.SelectSingleNode("Position[@key='Home']"));
                Stations = new List<Station>();
                foreach(XmlNode snode in manipulatorNode.SelectNodes("Stations/Station"))
                {
                    Stations.Add(new Station(snode));
                }
            }
        }

        public class Prealigner
        {
            public Prealigner(XmlNode prealignerNode)
            {

            }
        }

        static void RunScripts(bool useSeqNum, bool useCheckSum)
        {
            if(useSeqNum)
            {
                if (useCheckSum)
                    RunScriptWithSeqNumAndWithChecksum();
                else
                    RunScriptWithSeqNumAndWithoutChecksum();
            }
            else
            {
                if (useCheckSum)
                    RunScriptWithoutSeqNumAndWithChecksum();
                else
                    RunScriptWithoutseqNumAndWithoutCheckSum();
            }
        }
        static void RunScriptWithoutSeqNumAndWithChecksum()
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

        static void RunScriptWithSeqNumAndWithChecksumWithReferenceMessages()
        {
            Script robotScript = new Script(robotLoop);
            Script preAlignerScript = new Script(preAlignLoop);

            //Console.WriteLine("!---    Robot Initialization ----!");
            //robotScript.Execute("$,1,01,INIT,1,1,G,", true, true);
            robotScript.Execute("$,1,01,CSRV,1,", true, true);
            return;

            Console.WriteLine("\n!---    Robot Wafer Map ----!");
            robotScript.ExecuteControlInterLeavedWithReferenceCommands("$,1,12,MMAP,C01,02,A,0,", new List<string>() {
                "$,1,13,RMAP,C01,00,",
                "$,1,13,RMAP,C02,00,",
                "$,1,13,RMAP,P01,00,",
                "$,1,13,RMAP,S01,00,",
                "$,1,13,RMAP,S02,00,",
                "$,1,13,RMAP,S03,00,",
                "$,1,13,RMAP,C01,01,",
                "$,1,13,RMAP,C01,02,",
                "$,1,13,RMAP,C01,03,",
                "$,1,13,RMAP,C02,01,",
                "$,1,13,RMAP,C02,02,",
                "$,1,13,RMAP,C02,03,",
                });
        }

        static void RunScriptWithSeqNumAndWithChecksum()
        {
            Script robotScript = new Script(robotLoop);
            Script preAlignerScript = new Script(preAlignLoop);

            Console.WriteLine("!---    Robot Initialization ----!");
            robotScript.Execute("$,1,01,INIT,1,1,G,", true, true);

            Console.WriteLine("!---    Robot Initialization ----!");
            robotScript.Execute("$,1,02,INIT,1,1,G,", true, true);

            Console.WriteLine("\n!----    Pre-Aligner Initialization    -----!");
            preAlignerScript.Execute("$,2,01,INIT,1,1,G,", true, true);

            Console.WriteLine("\n!---    Robot Put ----!");
            robotScript.Execute("$,1,03,MTRS,P,P01,00,R,2,P4,00090000,", true, true);

            Console.WriteLine("\n!---    Robot Get ----!");
            robotScript.Execute("$,1,04,MTRS,G,S01,00,L,2,G4,00000250,-0000300,00000000,00000000,", true, true);

            Console.WriteLine("\n!---    Robot Get ----!");
            robotScript.Execute("$,1,05,MTRS,G,P01,00,L,1,G1,", true, true);

            Console.WriteLine("\n!---    Robot Motion Transfer Point ----!");
            robotScript.Execute("$,1,06,MPNT,G3,", true, true);

            Console.WriteLine("\n!---    Robot Continued Transfer ----!");
            robotScript.Execute("$,1,07,MCTR,P,P01,00,L,1,P4,00090000,", true, true);

            Console.WriteLine("\n!---    Robot Move To Specified Position ----!");
            robotScript.Execute("$,1,08,MTCH,C04,01,R,1,R,00000000,00000000,00000000,", true, true);

            Console.WriteLine("\n!---    Robot Move To Specified Position ----!");
            robotScript.Execute("$,1,09,MTCH,C01,01,L,1,B,", true, true);

            Console.WriteLine("\n!---    Robot Move Axis To Specified Position ----!");
            robotScript.Execute("$,1,10,MABS,H,1,C,-0000300,", true, true);

            Console.WriteLine("\n!---    Robot Move Axis To Specified Relative Position ----!");
            robotScript.Execute("$,1,11,MREL,H,1,C,-0000300,", true, true);

            Console.WriteLine("\n!---    Robot Wafer Map ----!");
            robotScript.Execute("$,1,12,MMAP,C01,02,A,0,", true, true);

            Console.WriteLine("\n!---    Robot Mapping Calibration ----!");
            robotScript.Execute("$,1,13,MMCA,C01,L,0,", true, true);



            Console.WriteLine("\n!---    Robot Deceleration/Stop ----!");

            robotScript.Execute("$,1,01,CSTP,H,", true, true);

            robotScript.Execute("$,1,01,CRSM,H,", true, true);

            robotScript.Execute("$,1,01,CSRV,1,", true, true);

            robotScript.Execute("$,1,01,CCLR,E,", true, true);

            robotScript.Execute("$,1,01,CSOL,F,0,0,", true, true);



            Console.WriteLine("\n!---    Robot Setting Commands ----!");

            robotScript.Execute("$,1,01,SSPD,3,L,G,00000999,", true, true);

            robotScript.Execute("$,1,01,SSLV,3,", true, true);

            robotScript.Execute("$,1,01,SPOS,V,N,C01,00,L,1,", true, true);

            robotScript.Execute("$,1,01,SABS,V,N,C01,L,1,00000100,00000200,00000300,00000400,00000500,", true, true);

            robotScript.Execute("$,1,01,SAPS,V,N,C01,L,1,00000100,00000200,00000300,", true, true);

            robotScript.Execute("$,1,01,SPDL,V,C01,L,F,", true, true);

            robotScript.Execute("$,1,01,SPSV,C01,L,F,", true, true);

            robotScript.Execute("$,1,01,SPLD,C01,F,F,", true, true);

            robotScript.Execute("$,1,01,SSTR,N,C01,71,00000900,", true, true);

            robotScript.Execute("$,1,01,SPRM,CRU,1111,000000000900,", true, true);

            robotScript.Execute("$,1,01,SMSK,ABCD,", true, true);

            robotScript.Execute("$,1,01,SSTD,G,", true, true);

            robotScript.Execute("$,1,01,STRM,0,0,0,0,0,", true, true);

            Console.WriteLine("\n!----    Robot Reference   -----!");

            robotScript.Execute("$,1,01,RSPD,0,L,R,", true, true);
            robotScript.Execute("$,1,01,RSLV,", true, true);
            robotScript.Execute("$,1,01,RPOS,R,", true, true);
            robotScript.Execute("$,1,01,RSTP,V,C01,01,L,1,R,", true, true);
            robotScript.Execute("$,1,01,RSTR,V,C01,00,", true, true);
            robotScript.Execute("$,1,01,RPRM,CIU,0001,", true, true);
            robotScript.Execute("$,1,01,RSTS,", true, true);
            robotScript.Execute("$,1,01,RERR,V,000,", true, true);
            robotScript.Execute("$,1,01,RMSK,", true, true);
            robotScript.Execute("$,1,01,RVER,", true, true);
            robotScript.Execute("$,1,13,RMAP,C01,00,", true, true);
            robotScript.Execute("$,1,13,RMPD,C01,", true, true);
            robotScript.Execute("$,1,13,RMCA,C01,", true, true);
            robotScript.Execute("$,1,13,RALN,", true, true);
            robotScript.Execute("$,1,13,RACA,", true, true);
            robotScript.Execute("$,1,13,RCCD,", true, true);
            robotScript.Execute("$,1,13,RTRM,", true, true);
            robotScript.Execute("$,1,13,RAWC,", true, true);
            robotScript.Execute("$,1,13,RLOG,00,", true, true);

            Console.WriteLine("\n\n ###########    PRE ALIGNER #############");

            Console.WriteLine("\n!----    Pre-Aligner Initialization    -----!");
            preAlignerScript.Execute("$,2,01,INIT,1,1,G,", true, true);

            Console.WriteLine("\n!----    Pre-Aligner Wafer Align    -----!");
            preAlignerScript.Execute("$,2,02,MALN,0,00000010,", true, true);

            Console.WriteLine("\n!----    Pre-Aligner Alignment calibration    -----!");
            preAlignerScript.Execute("$,2,03,MACA,0,", true, true);

            Console.WriteLine("\n!---    PreAligner Deceleration/Stop ----!");
            preAlignerScript.Execute("$,2,01,CSTP,H,", true, true);

            preAlignerScript.Execute("$,2,01,CRSM,H,", true, true);

            preAlignerScript.Execute("$,2,01,CSRV,1,", true, true);

            preAlignerScript.Execute("$,2,01,CCLR,E,", true, true);

            preAlignerScript.Execute("$,2,01,CSOL,F,0,0,", true, true);

            Console.WriteLine("\n!---    PreAligner Settings ----!");

            preAlignerScript.Execute("$,2,01,SMSK,ABCD,", true, true);
            preAlignerScript.Execute("$,1,01,SMSK,ABCD,", true, true);
            preAlignerScript.Execute("$,2,01,STRM,0,0,0,0,0,", true, true);


            preAlignerScript.Execute("$,2,01,SPRM,CRU,1111,000000000900,", true, true);

            preAlignerScript.Execute("$,2,01,SSLV,3,", true, true);

            preAlignerScript.Execute("$,2,01,SSPD,3,L,G,00000999,", true, true);

            preAlignerScript.Execute("$,2,01,SSTD,G,", true, true);

            Console.WriteLine("\n!---    PreAligner Reference ----!");

            preAlignerScript.Execute("$,2,01,RSPD,0,L,S,", true, true);
            preAlignerScript.Execute("$,2,01,RSLV,", true, true);
            preAlignerScript.Execute("$,2,01,RPOS,R,", true, true);
            preAlignerScript.Execute("$,2,01,RPRM,CIU,0001,", true, true);
            preAlignerScript.Execute("$,2,01,RSTS,", true, true);
            preAlignerScript.Execute("$,2,01,RERR,V,000,", true, true);
            preAlignerScript.Execute("$,2,01,RMSK,", true, true);
            preAlignerScript.Execute("$,2,01,RVER,", true, true);
            preAlignerScript.Execute("$,2,13,RALN,", true, true);
            preAlignerScript.Execute("$,2,13,RACA,", true, true);
            preAlignerScript.Execute("$,2,13,RCCD,", true, true);
            preAlignerScript.Execute("$,2,13,RLOG,00,", true, true);

        }

        static void RunScriptWithSeqNumAndWithoutChecksum()
        {
            Script robotScript = new Script(robotLoop);
            Script preAlignerScript = new Script(preAlignLoop);

            Console.WriteLine("!---    Robot Initialization ----!");
            robotScript.Execute("$,1,01,INIT,1,1,G,",false,true);

            Console.WriteLine("\n!---    Robot Get ----!");
            robotScript.Execute("$,1,02,MTRS,G,C02,05,L,1,G1,", false, true);

            Console.WriteLine("\n!---    Robot Put ----!");
            robotScript.Execute("$,1,03,MTRS,P,P01,00,R,2,P4,00090000,", false, true);

            Console.WriteLine("\n!---    Robot Get ----!");
            robotScript.Execute("$,1,04,MTRS,G,S01,00,L,2,G4,00000250,-0000300,00000000,00000000,", false, true);

            Console.WriteLine("\n!---    Robot Get ----!");
            robotScript.Execute("$,1,05,MTRS,G,P01,00,L,1,G1,", false, true);

            Console.WriteLine("\n!---    Robot Motion Transfer Point ----!");
            robotScript.Execute("$,1,06,MPNT,G3,", false, true);

            Console.WriteLine("\n!---    Robot Continued Transfer ----!");
            robotScript.Execute("$,1,07,MCTR,P,P01,00,L,1,P4,00090000,", false, true);

            Console.WriteLine("\n!---    Robot Move To Specified Position ----!");
            robotScript.Execute("$,1,08,MTCH,C04,01,R,1,R,00000000,00000000,00000000,", false, true);

            Console.WriteLine("\n!---    Robot Move To Specified Position ----!");
            robotScript.Execute("$,1,09,MTCH,C01,01,L,1,B,", false, true);

            Console.WriteLine("\n!---    Robot Move Axis To Specified Position ----!");
            robotScript.Execute("$,1,10,MABS,H,1,C,-0000300,", false, true);

            Console.WriteLine("\n!---    Robot Move Axis To Specified Relative Position ----!");
            robotScript.Execute("$,1,11,MREL,H,1,C,-0000300,", false, true);

            Console.WriteLine("\n!---    Robot Wafer Map ----!");
            robotScript.Execute("$,1,12,MMAP,C01,03,A,0,", false, true);

            Console.WriteLine("\n!---    Robot Mapping Calibration ----!");
            robotScript.Execute("$,1,13,MMCA,C01,L,0,", false, true);


            Console.WriteLine("\n\n ###########    PRE ALIGNER #############");

            Console.WriteLine("\n!----    Pre-Aligner Initialization    -----!");
            preAlignerScript.Execute("$,2,01,INIT,1,1,G,", false, true);

            Console.WriteLine("\n!----    Pre-Aligner Wafer Align    -----!");
            preAlignerScript.Execute("$,2,02,MALN,0,00000010,", false, true);

            Console.WriteLine("\n!----    Pre-Aligner Alignment calibration    -----!");
            preAlignerScript.Execute("$,2,03,MACA,0,", false, true);
        }

        static void RunScriptWithoutseqNumAndWithoutCheckSum()
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

