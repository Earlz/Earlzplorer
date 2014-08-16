using System;
using System.IO;
using Bitnet.Client;

namespace Earlz.Earlzplorer
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            if(args.Length==0)
            {
            Console.WriteLine(@"Usage: earlzplorer coin.conf rpcport command
Commands:
summarizeblocks begin-end
summarizeblocktransactions blocknum
getblockbynumber blocknum
dumpblockswithtx begin-end
dumpblockwithtx blocknum
richlist [untilblock]
richminers [begin-end] (defaults to last 10,000 blocks)

note: The best time can be had by redirecting output to a file, and then using a text editor to skim the file

");
                return;
            }

            var config=File.ReadAllLines(args[0]);
            string username=null;
            string password=null;
            foreach(var eachline in config)
            {
                var line=eachline;
                line=line.Trim();
                if(line.StartsWith("rpcuser="))
                {
                    username=line.Substring("rpcuser=".Length);
                }
                if(line.StartsWith("rpcpassword="))
                {
                    password=line.Substring("rpcpassword=".Length);
                }
            }

            var bc=new BitnetClient("http://127.0.0.1:"+args[1], username, password);
            var s=new Summarizer(bc);
            Range r;
            int num;
            switch(args[2])
            {
                case "summarizeblocks":
                r=ParseRange(args);
                s.SummarizeBlocks(r.Begin,r.End);
                break;
            case "dumpblockswithtx":
                r=ParseRange(args);
                s.DumpBlocksWithTx(r.Begin, r.End);
                break;
            case "dumpblockwithtx":
                num=int.Parse(args[3]);
                s.DumpBlocksWithTx(num, num);
                break;
            default: 
                Console.WriteLine("Unknown command");
                return;
            }

        }
        struct Range
        {
            public int Begin;
            public int End;
        }
        static Range ParseRange(string[] args)
        {
            string range=args[3];
            if(!range.Contains("-"))
            {
                throw new ArgumentParseException("range expected for this command: begin-end, ex. 1-100");
            }
            var tmp=range.Split(new char[]{'-'});
            int begin=int.Parse(tmp[0]);
            int end=int.Parse(tmp[1]);
            return new Range(){Begin=begin, End=end};
        }
    }
}
