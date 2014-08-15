using System;
using System.IO;
using Bitnet.Client;

namespace Earlz.Earlzplorer
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(@"Usage: earlzplorer coin.conf rpcport command
Commands:
summarizeblocks begin-end
summarizeblocktransactions blocknum
getblockbynumber blocknum

note: The best time can be had by redirecting output to a file, and then using a text editor to skim the file

");

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

            if(args[2]=="summarizeblocks")
            {
                for(int i=0;i<100;i++)
                {
                    var hash=bc.InvokeMethod("getblockhash", i)["result"];
                    var info=bc.InvokeMethod("getblock", hash)["result"];
                    Console.WriteLine(info);
                    foreach(var t in info["tx"])
                    {
                        var txcode=bc.InvokeMethod("getrawtransaction", t, 1)["result"];
                        if(txcode==null)
                        {
                            throw new NotSupportedException("apparently doesn't support getrawtransaction. such shitcoin");
                        }
                        Console.WriteLine(txcode);
                        //var decoded=bc.InvokeMethod("decoderawtransaction", txcode);
                    }
                }
            }

        }
    }
}
