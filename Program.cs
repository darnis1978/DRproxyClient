using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using FiscalModel;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

var isConnected = false;
var sendMessage = false;
//string clientID = args[0];


Console.WriteLine("-------------------------------------------------");
Console.WriteLine("-> run client..... [clinetId:" + "1" + "]");

// using HttpClient client = new(new HttpClientHandler
// {
//     ClientCertificateOptions = ClientCertificateOption.Manual,
//     SslProtocols = SslProtocols.Tls12,
//     ClientCertificates = { new X509Certificate2("D192.168.1.43.crt", "D192.168.1.43.key") }
// });

HttpClient client = new();

//-----------------------------------------------------------------
// SingalR Handling
//-----------------------------------------------------------------
isConnected = true;
var hubCon = new HubConnectionBuilder()
   .WithUrl("https://192.168.1.43:7171/connectort")
   .WithAutomaticReconnect()
   .Build();

// waiting for server feedback
hubCon.On<string>("ReceiveMessage", (message) =>
{
   var newMessage = $"-< Confirmation {message}";
   Console.WriteLine(newMessage);
   Console.WriteLine("=================================================");
   Console.WriteLine("");
});

// start signalR client
hubCon.StartAsync().ContinueWith(task => {
      Console.WriteLine("-> connecting to message server .......");
   if (task.IsFaulted) {
      Console.WriteLine("There was an error opening the connection:{0}");
   } else {
      Console.WriteLine("-> connected");
      Console.WriteLine("-------------------------------------------------");
      hubCon.InvokeAsync< string >("RegisterClient", "1").Wait();
      isConnected = true;

   }
}).Wait();                                    
//-----------------------------------------------------------------


//-----------------------------------------------------------------
// Sending TX to DRProxy
//-----------------------------------------------------------------
async Task<bool> ProcessRepositoriesAsync(HttpClient client) {
   //String payload = "{\"CustomerId\": 5,\"CustomerName\": \"Pepsi\"}";
   string payload;


   using (StreamReader r = new StreamReader("zwykly_rabat.json"))
   {
      payload = r.ReadToEnd();

   }

   // string strPayload = JsonSerializer.Serialize(payload);

  CustomerModel strPayload = CustomerModel.FromJson(payload);

   // string strPayload = JsonSerializer.Serialize(payload);

   var response = await client.PostAsync("https://192.168.1.43:7171/api/Receipt", new StringContent(strPayload.ToJson(), Encoding.UTF8, "application/json"));
   var contents = await response.Content.ReadAsStringAsync();

   if (response.StatusCode == System.Net.HttpStatusCode.OK){
      Console.WriteLine("-< Response (200): " + contents);
      DRfiscalResponse_PEPCO re = JsonSerializer.Deserialize<DRfiscalResponse_PEPCO>(contents);
       Console.WriteLine("-<" + re.UID);

      return true;
   }
   else {
      Console.WriteLine("-< Response (FAILED)");
      return false;
   }
}

//-----------------------------------------------------------------
// Main Loop
//-----------------------------------------------------------------
 while(true){

    Thread.Sleep(1000);
  if (isConnected == true && sendMessage == false){
         Console.WriteLine("-> Sending transaction to DRProxy");
       sendMessage = await ProcessRepositoriesAsync(client);
         
   }

 }


