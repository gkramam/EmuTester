using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmuTester
{
    public class Script
    {
        ScriptState _scriptState = ScriptState.None;
        ConnectionWoker _worker;
        System.Timers.Timer retryTimer;
        string previousCommand = string.Empty;
        ManualResetEvent _signalStateChange;
        bool _useCheckSum;
        int _seqNum = -1;
        bool _useSeqNum;

        BlockingCollection<string> IncomingQ = new BlockingCollection<string>();

        public Script(ConnectionWoker worker)
        {
            _signalStateChange = new ManualResetEvent(false);
            _worker = worker;
            retryTimer = new System.Timers.Timer(10000);
            retryTimer.Enabled = false;
            retryTimer.AutoReset = false;
            retryTimer.Elapsed += MessageTimer_Elapsed; ;

            Task.Run(() => 
            {
                foreach(var res in IncomingQ.GetConsumingEnumerable())
                {
                    Process(res);
                }
            });
        }

        private void MessageTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //retryTimer.Stop();
            //if(_scriptState == ScriptState.CommandSent)
            //{
            //    Write(previousCommand, () => { });
            //}
            //if(_scriptState == ScriptState.ACKNSent)
            //{
            //    ;
            //}
        }

        public void ExecuteWithoutCheckSum(string message)
        {
            Execute(message, false);
        }

        void IncrementSeQNum()
        {
            _seqNum++;
            if (_seqNum > 99)
                _seqNum = 0;
        }

        bool isInterLeaved = false;
        public void ExecuteControlInterLeavedWithReferenceCommands(string controlMsg,List<string> referenceMsgList)
        {
            isInterLeaved = true;
            _worker.PostReadCallback = (x) => { IncomingQ.Add(x); };


            Write(ComposeFinalString(controlMsg), () => { _scriptState = ScriptState.CommandSent; });
            referenceMsgList.ForEach(m => { Write(ComposeFinalString(m),()=> { }); });

            while (true)
            {
                _signalStateChange.WaitOne();
                if (_scriptState == ScriptState.ACKNSent)
                {
                    //Thread.Sleep(15);
                    break;
                }
                else
                    _signalStateChange.Reset();
            }
        }

        string ComposeFinalString(string message)
        {
            _useSeqNum = true;
            _useCheckSum = true; ;

            string command = string.Empty;

            if (_useSeqNum)
            {
                IncrementSeQNum();
                message = message.Remove(4, 2);
                message = message.Insert(4, _seqNum.ToString("D2"));
            }

            if (_useCheckSum)
            {
                string strippedMsg = message.Substring(1, message.Length - 1);
                string chksum = CheckSum.Compute(strippedMsg);
                command = $"{message}{chksum}\r";
            }
            else
                command = $"{message.Remove(message.Length - 1)}\r";

            return command;
        }

        public void Execute(string message, bool useCheckSum = true, bool useSeqNum=false)
        {
            _useSeqNum = useSeqNum;
            _useCheckSum = useCheckSum;

            string command = string.Empty;

            if(_useSeqNum)
            {
                IncrementSeQNum();
                message = message.Remove(4, 2);
                message = message.Insert(4, _seqNum.ToString("D2"));
            }

            if (_useCheckSum)
            {
                string strippedMsg = message.Substring(1, message.Length - 1);
                string chksum = CheckSum.Compute(strippedMsg);
                command = $"{message}{chksum}\r";
            }
            else
                command = $"{message.Remove(message.Length-1)}\r";

            _worker.PostReadCallback = (x)=> { IncomingQ.Add(x); };
            //_worker.Write(command, ()=> { _scriptState = ScriptState.CommandSent; });
            Write(command, () => { _scriptState = ScriptState.CommandSent; });

            while (true)
            {
                _signalStateChange.WaitOne();
                if (_scriptState == ScriptState.ACKNSent)
                {
                    Thread.Sleep(15);
                    break;
                }
                else
                    _signalStateChange.Reset();
            }
        }

        void Write(string cmd,Action setState)
        {
            previousCommand = cmd;
            _worker.Write(cmd, setState);
            retryTimer.Start();
            _signalStateChange.Set();
        }

        void Process(string cmdstr)
        {
            retryTimer.Stop();
            bool seqNumPresent = false;
            var checkSum = cmdstr.Substring(cmdstr.Length - 1 - 2, 2);
            string strippedCmd = cmdstr.Substring(1, cmdstr.Length - 1 - 3);

            int unit = Convert.ToInt32(cmdstr.Substring(2, 1));
            var fields = cmdstr.Split(',');
            var cmdName = string.Empty;
            if (fields[5].Length == 6)
            {
                cmdName = fields[4];
            }
            else
            {
                cmdName = fields[5];
            }

            if (cmdstr.StartsWith("!"))//End Of Execution Message
            {
                //Console.WriteLine($"Received End of Execution : {cmdstr}");
                Program.ConsoleQ.Add($"Received End of Execution : {cmdstr}");
                _scriptState = ScriptState.EndOfExecReceived;

                if (fields[5].Length == 6)
                {
                    cmdName = fields[4];
                }
                else
                {
                    seqNumPresent = true;
                    cmdName = fields[5];
                    IncrementSeQNum();
                }

                //Send ACKN
                string acknmsg = seqNumPresent? $"$,{unit},{_seqNum.ToString("D2")},ACKN" : $"$,{unit},ACKN";
                string command = string.Empty;
                if (_useCheckSum)
                {
                    string strippedMsg = acknmsg.Substring(1, acknmsg.Length - 1) + ',';
                    string chksum = CheckSum.Compute(strippedMsg);
                    command = $"{acknmsg},{chksum}\r";
                }
                else
                    command = $"{acknmsg}\r";
                //_worker.Write(command, () => { _scriptState = ScriptState.ACKNSent; });
                Write(command, () => { _scriptState = ScriptState.ACKNSent; });
            }
            else if (cmdstr.StartsWith(">"))//Event
            {
                Program.ConsoleQ.Add($"Received Event         : {cmdstr}");
            }
            else if (cmdstr.StartsWith("?"))//Error
            {

            }
            else
            {
                //Console.WriteLine($"Received Response         : {cmdstr}");
                Program.ConsoleQ.Add($"Received Response         : {cmdstr}");
                switch(cmdName.First())
                {
                    case 'R':
                    case 'S':
                        if (!isInterLeaved)
                            _scriptState = ScriptState.ACKNSent;
                        else
                            _scriptState = ScriptState.ResponseReceived;
                        break;
                    case 'I':
                    case 'M':
                    case 'C':
                        _scriptState = ScriptState.ResponseReceived;
                        break;
                }
                _signalStateChange.Set();
            }

           // _signalStateChange.Set();
        }
    }

    public enum ScriptState
    {
        None,
        CommandSent,
        ResponseReceived,
        EndOfExecReceived,
        ACKNSent,
        ErrorReceived,
        EventReceived
    }

}
