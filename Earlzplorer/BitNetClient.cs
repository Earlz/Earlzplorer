// COPYRIGHT 2011 Konstantin Ineshin, Irkutsk, Russia.
// If you like this project please donate BTC 18TdCC4TwGN7PHyuRAm8XV88gcCmAHqGNs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace Bitnet.Client
{
    public class BitnetClient
    {
        public static BitnetClient Current
        {
            get;set;
        }
        public BitnetClient()
        { 
        }

        public BitnetClient(string a_sUri, string user, string pass)
        {
            Url = new Uri(a_sUri);
            Credentials=new NetworkCredential(user, pass);
        }

        public Uri Url;

        public ICredentials Credentials;

        public JObject InvokeMethod(string a_sMethod, params object[] a_params)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(Url);
            webRequest.Credentials = Credentials;

            webRequest.ContentType = "application/json-rpc";
            webRequest.Method = "POST";

            JObject joe = new JObject();
            joe["jsonrpc"] = "1.0";
            joe["id"] = "1";
            joe["method"] = a_sMethod;

            if (a_params != null) {
                if (a_params.Length > 0) {
                    JArray props = new JArray();
                    foreach (var p in a_params) {
                        props.Add(p);
                    }
                    joe.Add(new JProperty("params", props));
                }
            }

            string s = JsonConvert.SerializeObject(joe);
            // serialize json for the request
            byte[] byteArray = Encoding.UTF8.GetBytes(s);
            webRequest.ContentLength = byteArray.Length;

            using (Stream dataStream = webRequest.GetRequestStream()) {
                dataStream.Write(byteArray, 0, byteArray.Length);
            }
            string content="";
            try
            {
                using (WebResponse webResponse = webRequest.GetResponse()) {
                    using (Stream str = webResponse.GetResponseStream()) {
                        using (StreamReader sr = new StreamReader(str)) {
                            content=sr.ReadToEnd();
                        }
                    }
                }
            }
            catch (WebException wex)
            {
                content = new StreamReader(wex.Response.GetResponseStream())
                    .ReadToEnd();
            }
            //content=@"{""result"":{""hex"":""01000000b040ef53010000000000000000000000000000000000000000000000000000000000000000ffffffff0602e80302b003ffffffff0140420f00000000001976a914dfb321ee5756abd8f9def47efec22c7fe77ff10188ac00000000"",""txid"":""eeb6e100d484fb473b0f0306320d6e8f8e6467c99da58e4260fa1ccfd4ed41c1"",""version"":1,""time"":1408188592,""locktime"":0,""vin"":[{""coinbase"":""02e80302b003"",""sequence"":4294967295}],""vout"":[{""value"":0.01000000,""n"":0,""scriptPubKey"":{""asm"":""OP_DUP OP_HASH160 dfb321ee5756abd8f9def47efec22c7fe77ff101 OP_EQUALVERIFY OP_CHECKSIG"",""reqSigs"":1,""type"":""pubkeyhash"",""addresses"":[""DRXupPDjiUFJNW8drjxSJmMswHah7bgenm""]}}],""blockhash"":""0000000002341749f9a81e10b6c645f19c347cf85bbfc94d5443b83f4e511f43"",""confirmations"":846,""time"":1408188593,""blocktime"":1408188593},""error"":null,""id"":""1""}";
            return JsonConvert.DeserializeObject<JObject>(content);
        }

        public void BackupWallet(string a_destination)
        {
            InvokeMethod("backupwallet", a_destination);
        }

        public string GetAccount(string a_address)
        {
            return InvokeMethod("getaccount", a_address)["result"].ToString();
        }

        public string GetAccountAddress(string a_account)
        {
            return InvokeMethod("getaccountaddress", a_account)["result"].ToString();
        }

        public IEnumerable<string> GetAddressesByAccount(string a_account)
        {
            return from o in InvokeMethod("getaddressesbyaccount", a_account)["result"]
                select o.ToString();
        }

        public float GetBalance(string a_account = null, int a_minconf = 1)
        {
            if (a_account == null) {
                return (float)InvokeMethod("getbalance")["result"];
            }
            return (float)InvokeMethod("getbalance", a_account, a_minconf)["result"];
        }

        public string GetBlockByCount(int a_height)
        {
            return InvokeMethod("getblockbycount", a_height)["result"].ToString();
        }

        public int GetBlockCount()
        {
            return (int)InvokeMethod("getblockcount")["result"];
        }

        public int GetBlockNumber()
        {
            return (int)InvokeMethod("getblocknumber")["result"];
        }

        public int GetConnectionCount()
        {
            return (int)InvokeMethod("getconnectioncount")["result"];
        }

        public float GetDifficulty()
        {
            return (float)InvokeMethod("getdifficulty")["result"];
        }

        public bool GetGenerate()
        {
            return (bool)InvokeMethod("getgenerate")["result"];
        }

        public float GetHashesPerSec()
        {
            return (float)InvokeMethod("gethashespersec")["result"];
        }

        public JObject GetInfo()
        {
            return InvokeMethod("getinfo")["result"] as JObject;
        }

        public string GetNewAddress(string a_account)
        {
            return InvokeMethod("getnewaddress", a_account)["result"].ToString();
        }

        public float GetReceivedByAccount(string a_account, int a_minconf = 1)
        {
            return (float)InvokeMethod("getreceivedbyaccount", a_account, a_minconf)["result"];
        }

        public float GetReceivedByAddress(string a_address, int a_minconf = 1)
        {
            return (float)InvokeMethod("getreceivedbyaddress", a_address, a_minconf)["result"];
        }

        public JObject GetTransaction(string a_txid)
        {
            return InvokeMethod("gettransaction", a_txid)["result"] as JObject;
        }

        public JObject GetWork()
        {
            return InvokeMethod("getwork")["result"] as JObject;
        }

        public bool GetWork(string a_data)
        {
            return (bool)InvokeMethod("getwork", a_data)["result"];
        }

        public string Help(string a_command = "")
        {
            return InvokeMethod("help", a_command)["result"].ToString();
        }

        public JObject ListAccounts(int a_minconf = 1)
        {
            return InvokeMethod("listaccounts", a_minconf)["result"] as JObject;
        }

        public JArray ListReceivedByAccount(int a_minconf = 1, bool a_includeEmpty = false)
        {
            return InvokeMethod("listreceivedbyaccount", a_minconf, a_includeEmpty)["result"] as JArray;
        }

        public JArray ListReceivedByAddress(int a_minconf = 1, bool a_includeEmpty = false)
        {
            return InvokeMethod("listreceivedbyaddress", a_minconf, a_includeEmpty)["result"] as JArray;
        }

        public JArray ListTransactions(string a_account, int a_count = 10)
        {
            return InvokeMethod("listtransactions", a_account, a_count)["result"] as JArray;
        }
        public JArray ListUnspent(int minconf=1, int maxconf=999999)
        {
            return InvokeMethod("listunspent", minconf, maxconf)["result"] as JArray;
        }
        public bool Move(
            string a_fromAccount, 
            string a_toAccount, 
            float a_amount, 
            int a_minconf = 1, 
            string a_comment = ""
        )
        {
            return (bool)InvokeMethod(
                "move", 
                a_fromAccount, 
                a_toAccount, 
                a_amount, 
                a_minconf, 
                a_comment
            )["result"];
        }

        public string SendFrom(
            string a_fromAccount, 
            string a_toAddress, 
            float a_amount, 
            int a_minconf = 1, 
            string a_comment = "", 
            string a_commentTo = ""
        )
        {
            return InvokeMethod(
                "sendfrom", 
                a_fromAccount, 
                a_toAddress, 
                a_amount, 
                a_minconf, 
                a_comment, 
                a_commentTo
            )["result"].ToString();
        }

        public string SendToAddress(string a_address, float a_amount, string a_comment, string a_commentTo)
        {
            return InvokeMethod("sendtoaddress", a_address, a_amount, a_comment, a_commentTo)["result"].ToString();
        }

        public void SetAccount(string a_address, string a_account)
        {
            InvokeMethod("setaccount", a_address, a_account);
        }

        public void SetGenerate(bool a_generate, int a_genproclimit = 1)
        {
            InvokeMethod("setgenerate", a_generate, a_genproclimit);
        }

        public void Stop()
        {
            InvokeMethod("stop");
        }

        public bool ValidateAddress(string a_address)
        {
            return (InvokeMethod("validateaddress", a_address)["result"] as JObject)["isvalid"].ToString()=="true" ? true :false;
        }
    }
}
