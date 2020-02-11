using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EmuTester
{
    public class ScriptINIT
    {
        ScriptState _scriptState = ScriptState.None;
        ConnectionWoker _worker;

        public ScriptINIT(ConnectionWoker worker)
        {
            _worker = worker;
        }

        public void Execute(string initMessage)
        {
            //string initMessage = "$,1,INIT,1,1,G,16\r";
            _worker.PostReadCallback = Process;
            _worker.Write(initMessage,()=> { _scriptState = ScriptState.CommandSent; });
            
            while(_scriptState != ScriptState.ACKNSent)
            {
                Thread.Sleep(1000);
            }
        }

        void Process(string cmdstr)
        {
            var checkSum = cmdstr.Substring(cmdstr.Length - 1 - 2, 2);
            string strippedCmd = cmdstr.Substring(1, cmdstr.Length - 1 - 3);
            int unit = Convert.ToInt32(cmdstr.Substring(2, 1));
            var cmdName = cmdstr.Substring(12, 4);

            if (cmdstr.StartsWith("!"))//End Of Execution Message
            {
                Console.WriteLine($"Received End of Execution : {cmdstr}");
                _scriptState = ScriptState.EndOfExecReceived;
                //Send ACKN
                _worker.Write("$,1,ACKN,D2\r", () => { _scriptState = ScriptState.ACKNSent; });
            }
            else if (cmdstr.StartsWith("<"))//Event
            {

            }
            else if (cmdstr.StartsWith("?"))//Error
            {

            }
            else
            {
                _scriptState = ScriptState.ResponseReceived;
                Console.WriteLine($"Received Response : {cmdstr}");
                //Normal Response $
            }
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
