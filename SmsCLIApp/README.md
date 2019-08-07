This C# application is written to provide a CLI demonstration of Nexmo's Messaging API's sms capabilities
This application Has the following dependencies:
Mono Options library for command line option handling
jose-jwt Library to preform the creation of the Java Web Tokens (JWTs)
Bouncy Castle API to preform the RSA cryptography

#Requesting Balance Data
In order to request balance data from the app, navigate to where the nexmo executable lives and use the following CLI command

```
nexmo balance
```

altertnatively you can make this request using the -c or --command option

```
nexmo -c=balance
```

#Sending an SMS message
In order to send an sms you will need to have Your applications private key file along side the executable in a file called 'privateKey.pem'.
In order to make sms work with your app you will need to edit the app.config file (or nexmo.config file after compilation) - you will need to replace the 
'privateKey' configuration parameter with your application's private key and you will need to replace the 'APP_ID' configuration parameter with your appId.

After the CLI tool is compiled you can run it by navigating in the command line to the binary and using a command like so:

```
nexmo sms --to=15558675309 --from=15048144243 --text="Hello World \u0001F601\u0001F602"
```

Note: Phone numbers should exclude the leading '+' or "00" and should begin with the country code, the from phone number must be a phone number registered to your nexmo Application,
