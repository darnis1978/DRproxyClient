using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

var isConnected = false;
var sendMessage = false;
string clientID = args[0];


Console.WriteLine("my client ID: " + clientID);
using HttpClient client = new();

//-----------------------------------------------------------------
// SingalR Handling
//-----------------------------------------------------------------
var hubCon = new HubConnectionBuilder()
   .WithUrl("http://localhost:5130/connectort")
   .WithAutomaticReconnect()
   .Build();

hubCon.On<string>("ReceiveMessage", (message) =>
{
   var newMessage = $"{message}";
   Console.WriteLine(newMessage);
});

hubCon.StartAsync().ContinueWith(task => {
   if (task.IsFaulted) {
      Console.WriteLine("There was an error opening the connection:{0}");
   } else {
      Console.WriteLine("Connected");
      hubCon.InvokeAsync< string >("RegisterClient", clientID).Wait();
      isConnected = true;

   }
}).Wait();                                    
//-----------------------------------------------------------------

//-----------------------------------------------------------------
// Sending TX to DRProxy
//-----------------------------------------------------------------
async Task<bool> ProcessRepositoriesAsync(HttpClient client) {
   //String payload = "{\"CustomerId\": 5,\"CustomerName\": \"Pepsi\"}";
   var payload = new Dictionary<string, object>
   {
      {"TranactionNmbr", 1},
      {"StoreNmbr", 1},
      {"PosNmbr", clientID},
      {"CashierName", "Kasjer 1"}
   };
   string strPayload = JsonSerializer.Serialize(payload);
   var response = await client.PostAsync("http://localhost:5130/api/receipt", new StringContent(strPayload, Encoding.UTF8, "application/json"));
   //Console.WriteLine(response);
   var contents = await response.Content.ReadAsStringAsync();
   //Console.WriteLine(contents);

   if (response.StatusCode == System.Net.HttpStatusCode.OK){
      Console.WriteLine("---< Response (OK)");
      Console.WriteLine("---< Response Message:");
      Console.WriteLine(contents);
      return true;
   }
   else {
      Console.WriteLine("---< Response (FAILED)");
      return false;
   }
}

//-----------------------------------------------------------------
// Main Loop
//-----------------------------------------------------------------
while(true){

   Thread.Sleep(8000);
  // if (isConnected == true && sendMessage == false){
         Console.WriteLine("---> Sending transaction to DRProxy");
         sendMessage = await ProcessRepositoriesAsync(client);
  // }

};


