using Mono.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace Nexmo
{
    class Program
    {
        private const string MESSAGING_URL = @"https://api.nexmo.com/v0.1/messages";
        private const string GET_BALANCE_URL = @"https://rest.nexmo.com/account/get-balance";
        private const string SMS_COMMAND_NAME = "sms";
        private const string GET_BALANCE_COMMAND_NAME = "balance";        
        private const int SECONDS_EXPIRY = 3600;

        static void Main(string[] args)
        {
            try
            {
                var showHelp = false;
                var explicitSmsRequest = false;
                var explicitBalanceRequest = false;
                var command = string.Empty;
                var to = string.Empty;
                var from = string.Empty;
                var message = string.Empty;
                var apiKey = ConfigurationManager.AppSettings["API_KEY"];
                var apiSecret = ConfigurationManager.AppSettings["API_SECRET"];
                var appId = ConfigurationManager.AppSettings["APP_ID"];
                var privateKey = ConfigurationManager.AppSettings["privateKey"];

                if (args.Length > 0)
                {
                    if (args[0] == SMS_COMMAND_NAME)
                    {
                        explicitSmsRequest = true;
                    }

                    if (args[0] == GET_BALANCE_COMMAND_NAME)
                    {
                        explicitBalanceRequest = true;
                    }
                }

                var oSet = new OptionSet()
            {
                {string.Format("c|command=","Command you wish to use either {0} or {1}.",GET_BALANCE_COMMAND_NAME,SMS_COMMAND_NAME), v=> command = v},
                {"t|to=", "Number you would like to send your sms message to - required only for sms", v=> to = v },
                {"f|from=", "Number you would like to send your sms message from - required only for sms", v => from = v },
                {"m|text=", "Message you would like sent - required only for sms", v=>message = ConvertEscapedUnicodeCharecters(v)},
                {"h|help", "Show help prompts", v=> showHelp = v!=null}
            };

                var parsedArgs = oSet.Parse(args);

                if (showHelp)
                {
                    ShowHelp(oSet);
                    return;
                }

                if (string.IsNullOrEmpty(command) && !explicitBalanceRequest && !explicitSmsRequest)
                {
                    Console.WriteLine(string.Format("Command missing - please add a valid command, appropriate values are {0} or {1}. send argument -h or --help for help, hit return to exit", GET_BALANCE_COMMAND_NAME, SMS_COMMAND_NAME));
                    return;
                }
                else if (command == GET_BALANCE_COMMAND_NAME || explicitBalanceRequest)
                {
                    GetBalance(apiKey, apiSecret);
                }
                else if (command == SMS_COMMAND_NAME || explicitSmsRequest)
                {
                    if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(to) || string.IsNullOrEmpty(from))
                    {
                        Console.WriteLine("Detected sms command but you have not provided a value for the to, from, or message - see inputed values below, type -h or --help for help");
                        Console.WriteLine("Command = " + command);
                        Console.WriteLine("to = " + to);
                        Console.WriteLine("from = " + from);
                        Console.WriteLine("text = " + message);
                    }
                    else
                    {
                        SendSms(to, from, message, appId, privateKey);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            
        }

        private static void ShowHelp(OptionSet theSet)
        {
            Console.WriteLine("Usage: Check balance or send sms message");
            Console.WriteLine("Options:");
            theSet.WriteOptionDescriptions(Console.Out);
        }

        static void SendSms(string to, string from, string message, string appId, string privateKey)
        {
            var claims = GetClaimsList(appId);
            var token = TokenGenerator.GenerateToken(claims, privateKey);            
            var smsRequestObject = new SmsRequestObject(to, from, message);
            var requestPayload = JsonConvert.SerializeObject(smsRequestObject);
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(MESSAGING_URL);

            httpWebRequest.ContentType = "application/json";
            httpWebRequest.Accept = "application/json";
            httpWebRequest.Method = "POST";
            httpWebRequest.PreAuthenticate = true;
            httpWebRequest.Headers.Add("Authorization", "Bearer " + token);            
            Console.WriteLine(requestPayload);            

            using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream(),Encoding.Unicode))
            {
                streamWriter.Write(requestPayload);
            }

            var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

            using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
            {
                var result = streamReader.ReadToEnd();
                Console.WriteLine(result);
                Console.WriteLine("Message Sent");
            }                        
        }

        static void GetBalance(string apiKey, string apiSecret)
        {
            var requestURI = new Uri(GET_BALANCE_URL + string.Format("?api_key={0}&api_secret={1}", apiKey, apiSecret));            
            var request = WebRequest.Create(requestURI) as HttpWebRequest;
            var results = string.Empty;

            request.Method = "GET";
            request.ContentType = "text/xml";

            using (var response = request.GetResponse() as HttpWebResponse)
            {
                var reader = new StreamReader(response.GetResponseStream());
                results = reader.ReadToEnd();
            }
            var balance = JsonConvert.DeserializeObject<BalanceResponse>(results);

            Console.WriteLine(string.Format("Your Balance is: {0}, and your Current AutoReload Setting is: {1}", balance.Balance, balance.AutoReload));            
        }

        private static string ConvertEscapedUnicodeCharecters(string value)
        {
            return System.Text.RegularExpressions.Regex.Replace(
                value,
                @"\\u(?<Value>[a-zA-Z0-9]{8})",
                code => {
                    var theValue = int.Parse(code.Groups["Value"].Value, System.Globalization.NumberStyles.HexNumber);                    
                    var ret = char.ConvertFromUtf32(theValue);                    
                    return ret.ToString();
                });
        }

        private static List<Claim> GetClaimsList(string appId)
        {
            var t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            var iat = new Claim("iat", ((Int32)t.TotalSeconds).ToString(), ClaimValueTypes.Integer32);
            var application_id = new Claim("application_id", appId);
            var exp = new Claim("exp", ((Int32)(t.TotalSeconds + SECONDS_EXPIRY)).ToString(), ClaimValueTypes.Integer32);
            var jti = new Claim("jti", Guid.NewGuid().ToString());
            var claims = new List<Claim>() { iat, application_id, exp, jti };

            return claims;
        }
    }
}
