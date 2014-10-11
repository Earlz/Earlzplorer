using System;
using Bitnet.Client;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

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
            public string AddressHash="N/A";
        }
        string[] BlockSummaryDesc={
            "BlockNum", "Time", "Difficulty", "TxCount", "ValueIn", "ValueOut", 
            "Reward", "Fees", "Version", "Type", "AddressHash"};
        void OutputColumns()
        {
            Console.WriteLine("{0,-10} {1,-20} {2,-15} {3,-10} {4,-20} {5,-20} {6,-20} {7,-20} {8,-10} {9,-6} {10,-10}", BlockSummaryDesc);
        }
        void WriteSummaryLine(BlockSummary s)
        {
            Console.WriteLine("{0,-10} {1,-20} {2,-15} {3,-10} {4,-20} {5,-20} {6,-20} {7,-20} {8,-10} {9,-6} {10,-10}", s.BlockNum, s.Time, s.Difficulty, s.TxCount, s.ValueIn, s.ValueOut, s.Reward, s.Fees, s.Version, s.Type, s.AddressHash);
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
                List<string> PayoutAddresses=new List<string>(2);
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
                            var pubkey=vout["scriptPubKey"];
                            if(pubkey!=null)
                            {
                                foreach(var address in pubkey["addresses"])
                                {
                                    PayoutAddresses.Add(address.ToString());
                                }
                            }
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

                //this is by no means meant to be exact or secure. It is only for eye balling where rewards go for blocks and what pool it's going to
                string payouts=""; //ugh not possible with linq?!
                foreach(var addr in PayoutAddresses)
                {
                    payouts+=addr;
                }
                var hasher = new RIPEMD160Managed();
                s.AddressHash=BitConverter.ToString(hasher.ComputeHash(Encoding.ASCII.GetBytes(payouts))).Replace("-","").ToLower().Substring(0,6);
                
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

