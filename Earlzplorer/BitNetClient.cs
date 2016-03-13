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
using System.Text.RegularExpressions;


namespace Bitnet.Client
{
    public class BitnetClient
    {
        const int RETRY_AMOUNT = 3;
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
            Username=user;
            Password=pass;
        }

        public Uri Url;
        string Username;
        string Password;
        string AuthInfo;

        public JObject InvokeMethod(string a_sMethod, params object[] a_params)
        {
            for (int retries = 1; retries <= RETRY_AMOUNT; retries++)
            {
                try
                {
                    return InvokeMethodRaw(a_sMethod, a_params);
                }
                catch (Exception e)
                {
                    if (retries >= RETRY_AMOUNT)
                    {
                        //if it's the last execution..
                        throw;
                    }
                    //else swallow the exception
                }
            }
            throw new Exception("shouldn't get here..");
        }
        JObject InvokeMethodRaw(string a_sMethod, object[] a_params)
        {
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
            string content=null;
            if(AuthInfo==null)
            {
                AuthInfo = Username + ":" + Password;
                AuthInfo = Convert.ToBase64String(Encoding.Default.GetBytes(AuthInfo));
            }
            using (var client = new WebClient())
            {
                client.UseDefaultCredentials=false;
                //inject authorization, otherwise it will do an extra request to "discover" the authorization method needed. We already know it's basic
                client.Headers[HttpRequestHeader.Authorization] = string.Format("Basic {0}", AuthInfo);
                client.Headers["Content-Type"] = "application/json-rpc";
                try
                {
                    content = client.UploadString(Url, s);
                }
                catch (WebException e)
                {
                    Console.WriteLine("Error executing command {0}", a_sMethod);
                    string response = null;
                    if (e.Response != null)
                    {
                        var responseStream = e.Response.GetResponseStream();

                        if (responseStream != null)
                        {
                            using (var reader = new StreamReader(responseStream))
                            {
                                response = reader.ReadToEnd();
                            }
                        }
                    }
                    if (response != null)
                    {
                        Console.WriteLine("Server Error: {0}", response);
                    }
                    throw;
                }
            }
            try
            {
                return JsonConvert.DeserializeObject<JObject>(content);
            }
            catch(Exception e)
            {
                //some shit coins have a duplicate time field in their json
                var matches=Regex.Matches(content, "\\\"time\\\":[\\d]*,");
                if(matches.Count!=2)
                {
                    throw;
                }
                content = content.Replace(matches[1].Value, "");
                return JsonConvert.DeserializeObject<JObject>(content);
            }
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
