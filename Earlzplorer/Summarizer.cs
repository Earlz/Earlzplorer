using System;
using Bitnet.Client;

namespace Earlz.Earlzplorer
{
    public class Summarizer
    {
        BitnetClient Bit;
        public Summarizer(BitnetClient bc)
        {
            Bit=bc;
        }
        public void SummarizeBlocks(string[] args)
        {
            string range=args[3];
            if(!range.Contains("-"))
            {
                throw new ArgumentParseException("range expected after summarizeblocks: begin-end, ex. 1-100");
            }
            var tmp=range.Split(new char[]{'-'});
            int begin=int.Parse(tmp[0]);
            int end=int.Parse(tmp[1]);
            SummarizeBlocks(begin, end);
        }
        public void DumpBlocksWithTx(int begin, int end)
        {
            for(int i=begin;i<end;i++)
            {
                var hash=Bit.InvokeMethod("getblockhash", i)["result"];
                var info=Bit.InvokeMethod("getblock", hash)["result"];
                Console.WriteLine(info);
                foreach(var t in info["tx"])
                {
                    var txcode=Bit.InvokeMethod("getrawtransaction", t, 1)["result"];
                    if(txcode==null)
                    {
                        throw new NotSupportedException("apparently doesn't support getrawtransaction. such shitcoin");
                    }
                    Console.WriteLine(txcode);
                }
            }
        }
        class BlockSummary
        {
            public int BlockNum;
            public string Time;
            public decimal Difficulty;
            public decimal ValueIn;
            public decimal ValueOut;
            public decimal Reward;
            public decimal Fees;
            public decimal Version;
        }
        string[] BlockSummaryDesc={
            "BlockNum", "Time", "Difficulty", "ValueIn", "ValueOut", 
            "Reward", "Fees", "Version"};
        void OutputColumns()
        {
            Console.WriteLine("{0,-10} {1,-25} {2,-10} {3,-10} {4,-10} {5,-10} {6,-10} {7,-10}", BlockSummaryDesc);
        }
        void WriteSummaryLine(BlockSummary s)
        {
            Console.WriteLine("{0,-10} {1,-25} {2,-10} {3,-10} {4,-10} {5,-10} {6,-10} {7,-10}", s.BlockNum, s.Time, s.Difficulty, s.ValueIn, s.ValueOut, s.Reward, s.Fees, s.Version);
        }
        public void SummarizeBlocks(int begin, int end)
        {
            //columns: blocknum, time, difficulty, valuein, valueout, reward, fees, version 
            OutputColumns();
            for(int i=begin;i<end;i++)
            {
                var s=new BlockSummary();
                var hash=Bit.InvokeMethod("getblockhash", i)["result"];
                var info=Bit.InvokeMethod("getblock", hash)["result"];
                s.BlockNum=info["height"].ToObject<int>();
                s.Version=info["version"].ToObject<int>();
                s.Time=info["time"].ToObject<long>().AsUnixTimestamp().ToString();
                s.Difficulty=info["difficulty"].ToObject<decimal>();
                foreach(var t in info["tx"])
                {
                    var txcode=Bit.InvokeMethod("getrawtransaction", t, 1)["result"];

                    if(txcode==null)
                    {
                        throw new NotSupportedException("apparently doesn't support getrawtransaction. such shitcoin");
                    }
                }
                WriteSummaryLine(s);
            }
        }
    }
}

