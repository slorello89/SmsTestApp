This C# application is written to provide a CLI demonstration of Nexmo's Messaging API's sms capabilities
This application Has the following dependencies:
* Mono Options library for command line option handling
* jose-jwt Library to preform the creation of the Java Web Tokens (JWTs)
* Bouncy Castle API to preform the RSA cryptography
* Newtonsoft Json library for serializing / deseralizing JSON

# Requesting Balance Data
In order to request balance data from the app, navigate to where the nexmo executable lives and use the following CLI command

```
nexmo balance
```

altertnatively you can make this request using the -c or --command option

```
nexmo -c=balance
```

# Sending an SMS message
In order to make sms work with your app you will need to edit the app.config file (or nexmo.exe.config file after compilation) - you will need to replace the 
'privateKey' configuration parameter with your application's private key and you will need to replace the 'APP_ID' configuration parameter with your appId.

After the CLI tool is compiled you can run it by navigating in the command line to the binary and using a command like so:

```
nexmo sms --to=15558675309 --from=15048144243 --text="Hello World \u0001F601\u0001F602"
```

Note: Phone numbers should exclude the leading '+' or "00" and should begin with the country code, the from phone number must be a phone number registered to your nexmo Application. Emojis should be sent by escpaing the charecter and passing in it's full Unicode encoding (eg \u0001F601)

# How The SMS is sent

Sending an SMS message consists of three steps
* Step 1: Generating an appropriate JWT 
* Step 2: Package the Data into an appropriate JSON format for the post command
* Step 3: Create web Request and send the request onto the server

## Step 1 Generate JWT:
In order to generate a JWT you will need to have an application that is registered and configured with Nexmo, you will need to generate a set of claims like so
```csharp
var t = DateTime.UtcNow - new DateTime(1970, 1, 1);
var iat = new Claim("iat", ((Int32)t.TotalSeconds).ToString(), ClaimValueTypes.Integer32); // Unix Timestamp for right now
var application_id = new Claim("application_id", appId); // Current app ID
var exp = new Claim("exp", ((Int32)(t.TotalSeconds + SECONDS_EXPIRY)).ToString(), ClaimValueTypes.Integer32); // Unix timestamp for when the token expires
var jti = new Claim("jti", Guid.NewGuid().ToString()); // Unique 
```

these claims will be encoded into your JWT and sent as your authentication for using the API

After generating the claims set you will then generate your token by signing these claims with your applications private key

```csharp
public static string GenerateToken(List<Claim> claims, string privateKey)
        {   
            RSAParameters rsaParams;
            //var privateKey = File.ReadAllText(PATH_TO_PRIVATE_KEY);
            using (var tr = new StringReader(privateKey))
            {
                var pemReader = new PemReader(tr);
                var kp = pemReader.ReadObject();                
                var privateRsaParams = kp as RsaPrivateCrtKeyParameters;
                rsaParams = DotNetUtilities.ToRSAParameters(privateRsaParams);
            }
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(rsaParams);
                Dictionary<string, object> payload = claims.ToDictionary(k => k.Type, v => (object)v.Value);
                return Jose.JWT.Encode(payload, rsa, Jose.JwsAlgorithm.RS256);
            }            
        }
```

## Step 2 Package the Data into an appropraite JSON format for the post command

To package the data up we will be using a very basic set of C# objects that are dressed with JSON Serialization attributes:

```csharp
using Newtonsoft.Json;

namespace Nexmo
{
    public class SmsRequestObject
    {
        [JsonProperty("to")]
        public To To { get; set; }

        [JsonProperty("from")]
        public From From { get; set; }

        [JsonProperty("message")]
        public Message Message { get; set; }

        public SmsRequestObject(string to, string from, string message)
        {
            this.To = new To(to);
            this.From = new From(from);
            this.Message = new Message(message);
        }
    }

    public class To
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        public To(string number)
        {
            this.Type = "sms";
            this.Number = number;
        }
    }

    public class From
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        public From(string number)
        {
            this.Type = "sms";
            this.Number = number;
        }
    }    

    public class Message
    {
        [JsonProperty("content")]
        public Content Content { get; set; }

        public Message(string text)
        {
            Content = new Content(text);
        }
    }

    public class Content
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        public Content(string text)
        {
            Type = "text";
            this.Text = text;
        }
    }
}
```

This makes converting your data into a JSON string as simple as initalizing a SmsRequestObject and using the Newtonsoft.Json library's Serialize method:

```csharp
var smsRequestObject = new SmsRequestObject(to, from, message);
var requestPayload = JsonConvert.SerializeObject(smsRequestObject);
```

## Step 3 Creating and Sending the Messaging API request
From here we are now ready to send our Messaging API sms request across the wire 

First off - the request URL for The Messaging api is:

```
private const string MESSAGING_URL = @"https://api.nexmo.com/v0.1/messages";
```
Armed with this we can take our Request Payload and token and turn it into an HttpWebRequest:

```csharp
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
```

and now that the request is generated all that's left to do is send it and print out the response!

```csharp
var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
{
    var result = streamReader.ReadToEnd();
    Console.WriteLine(result);
    Console.WriteLine("Message Sent");
}         
```
et voila the SMS message is sent on

# Using the GetBalance API

The get Balance API is considerably simpler - after you have your API_Key/API_Secret all that's really needed is to create a web Request, add those parameters to the query string in the URL and fire it off, This sample uses a POCO to deseralize the response into for ease of use

You will be sending the get balance request to 

```csharp
private const string GET_BALANCE_URL = @"https://rest.nexmo.com/account/get-balance";
```

the logic for sending the request and handling the result is pretty straight forward:

```csharp
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
```