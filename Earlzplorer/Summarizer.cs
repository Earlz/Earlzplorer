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
            public string Type="PoW";
        }
        string[] BlockSummaryDesc={
            "BlockNum", "Time", "Difficulty", "TxCount", "ValueIn", "ValueOut", 
            "Reward", "Fees", "Version", "Type"};
        void OutputColumns()
        {
            Console.WriteLine("{0,-10} {1,-20} {2,-15} {3,-10} {4,-20} {5,-20} {6,-20} {7,-20} {8,-10} {9,-6}", BlockSummaryDesc);
        }
        void WriteSummaryLine(BlockSummary s)
        {
            Console.WriteLine("{0,-10} {1,-20} {2,-15} {3,-10} {4,-20} {5,-20} {6,-20} {7,-20} {8,-10} {9,-6}", s.BlockNum, s.Time, s.Difficulty, s.TxCount, s.ValueIn, s.ValueOut, s.Reward, s.Fees, s.Version, s.Type);
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
                decimal fees=0M;
                decimal reward=0M;
                decimal valuein=0M;
                decimal valueout=0M;
                //info about the genesis transaction is difficult to get at
                if(i==0)
                {
                    WriteSummaryLine(s);
                    continue;
                }
                int txn=0;
                foreach(var t in info["tx"])
                {
                    var txcode=Bit.InvokeMethod("getrawtransaction", t, 1)["result"];
                    if(txcode==null)
                    {
                        throw new NotSupportedException("apparently doesn't support getrawtransaction. such shitcoin");
                    }
                    bool coinbase=false;
                    decimal txin=0M;
                    decimal txout=0M;
                    foreach(var vin in txcode["vin"])
                    {
                        if(vin["coinbase"] != null)
                        {
                            coinbase=true;
                            //mined
                            continue;
                        }
                        txin+=GetValueOut(vin["txid"].ToString(), vin["vout"].ToObject<int>());
                    }
                    foreach(var vout in txcode["vout"])
                    {
                        decimal tmp=vout["value"].ToObject<decimal>();
                        if(coinbase)
                        {
                            reward+=tmp;
                        }
                        txout+=tmp;

                    }
                    if(!coinbase)
                    {
                        decimal txfees=txin-txout;
                        if(txfees<0)
                        {
                            if(txn==1 && 
                                decimal.Parse(txcode["vout"][0]["value"].ToString())==0M &&
                                txcode["vout"][0]["scriptPubKey"]["asm"].ToString()=="" &&
                                reward==0)
                            {
                                //PoS reward
                                reward+=-txfees;
                                txfees=0;
                                s.Type="PoS";
                            }
                            else
                            {
                                throw new ApplicationException("This transaction appears to have negative fees. This could mean the network has an exploit, or that this code can't handle some portion of it");
                            }
                        }
                        fees=txfees;
                    }
                    s.TxCount++;
                    valuein+=txin;
                    valueout+=txout;
                    txn++;
                }
                s.ValueIn=valuein;
                s.ValueOut=valueout;
                s.Fees=fees;
                s.Reward=reward-fees;
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

