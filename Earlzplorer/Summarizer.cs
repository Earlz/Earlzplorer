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
            public int TxCount;
            public decimal ValueIn;
            public decimal ValueOut;
            public decimal Reward;
            public decimal Fees;
            public decimal Version;
        }
        string[] BlockSummaryDesc={
            "BlockNum", "Time", "Difficulty", "TxCount", "ValueIn", "ValueOut", 
            "Reward", "Fees", "Version"};
        void OutputColumns()
        {
            Console.WriteLine("{0,-10} {1,-20} {2,-10} {3,-10} {4,-10} {5,-10} {6,-10} {7,-10} {8,-10}", BlockSummaryDesc);
        }
        void WriteSummaryLine(BlockSummary s)
        {
            Console.WriteLine("{0,-10} {1,-20} {2,-10} {3,-10} {4,-10} {5,-10} {6,-10} {7,-10} {8,-10}", s.BlockNum, s.Time, s.Difficulty, s.TxCount, s.ValueIn, s.ValueOut, s.Reward, s.Fees, s.Version);
        }
        public void SummarizeBlocks(int begin, int end)
        {
            //columns: blocknum, time, difficulty, valuein, valueout, reward, fees, version 
            OutputColumns();
            for(int i=end;i>=begin;i--)
            {
                var s=new BlockSummary();
                var hash=Bit.InvokeMethod("getblockhash", i)["result"];
                var info=Bit.InvokeMethod("getblock", hash)["result"];
                s.BlockNum=info["height"].ToObject<int>();
                s.Version=info["version"].ToObject<int>();
                s.Time=info["time"].ToObject<long>().AsUnixTimestamp().ToUniversalTime().ToString("MM/dd/yyyy HH:mm:ss");
                s.Difficulty=info["difficulty"].ToObject<decimal>();
                //tallies
                decimal fees;
                decimal reward;
                decimal valuein=0M;
                decimal valueout=0M;
                //info about the genesis transaction is difficult to get at
                if(i==0)
                {
                    WriteSummaryLine(s);
                    continue;
                }
                foreach(var t in info["tx"])
                {
                    var txcode=Bit.InvokeMethod("getrawtransaction", t, 1)["result"];
                    if(txcode==null)
                    {
                        throw new NotSupportedException("apparently doesn't support getrawtransaction. such shitcoin");
                    }
                    foreach(var vin in txcode["vin"])
                    {
                        if(vin["coinbase"] != null)
                        {
                            //mined
                            continue;
                        }

                        valuein+=GetValueOut(vin["txid"].ToString(), vin["vout"].ToObject<int>());
                    }
                    foreach(var vout in txcode["vout"])
                    {
                        valueout+=vout["value"].ToObject<decimal>();
                    }
                    s.TxCount++;
                }
                s.ValueIn=valuein;
                s.ValueOut=valueout;
                WriteSummaryLine(s);
            }
        }
        decimal GetValueOut(string txid, int vout)
        { 
            var txcode=Bit.InvokeMethod("getrawtransaction", txid, 1)["result"];
            if(txcode==null)
            {
                throw new NotSupportedException("apparently doesn't support getrawtransaction. such shitcoin");
            }
            return txcode["vout"][vout]["value"].ToObject<decimal>();
        }

    }
}

