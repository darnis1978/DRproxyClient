using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;
using FiscalModel;
using System.Security.Cryptography.X509Certificates;

var isConnected = false;
var sendMessage = false;
string clientID = "001";
string serverUrl = "https://192.168.1.43:7171";


Console.WriteLine("-------------------------------------------------");
Console.WriteLine("-> run client..... [clinetId:" + clientID + "]");

HttpClientHandler handler = new();
handler.ServerCertificateCustomValidationCallback = (m, c, ch, errors) => true;
try
{
   handler.ClientCertificates.Add(new X509Certificate2("192.168.1.43.cert", "192.168.1.43.key"));
}
catch (Exception ex)
{
   Console.WriteLine($"-> Warning: Could not load client certificate for HttpClient: {ex.Message}");
}

HttpClient client = new(handler);

//-----------------------------------------------------------------
// SingalR Handling
//-----------------------------------------------------------------
isConnected = true;


var hubCon = new HubConnectionBuilder()
   .WithUrl(serverUrl + "/connectort", options =>
   {
      options.HttpMessageHandlerFactory = (handler) =>
      {
         if (handler is HttpClientHandler clientHandler)
         {
            // Pomijamy błędy walidacji certyfikatu serwera (np. dla self-signed)
            clientHandler.ServerCertificateCustomValidationCallback = (m, c, ch, errors) => true;

            // Załaduj i dołącz certyfikat klienta (jeśli serwer go wymaga)
            try
            {
               clientHandler.ClientCertificates.Add(new X509Certificate2("192.168.1.43.cert", "192.168.1.43.key"));
            }
            catch (Exception ex)
            {
               Console.WriteLine($"-> Warning: Could not load client certificate: {ex.Message}");
            }
         }
         return handler;
      };
   })
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
hubCon.StartAsync().ContinueWith(task =>
{
   Console.WriteLine("-> connecting to message server .......");
   if (task.IsFaulted)
   {
      Console.WriteLine("There was an error opening the connection:{0}");
   }
   else
   {

      Console.WriteLine("-> connected");
      Console.WriteLine("-------------------------------------------------");
      hubCon.InvokeAsync<string>("RegisterClient", clientID).Wait();
      isConnected = true;

   }
}).Wait();

//-----------------------------------------------------------------


//-----------------------------------------------------------------
// Sending TX to DRProxy
//-----------------------------------------------------------------
async Task<bool> ProcessRepositoriesAsync(HttpClient client)
{
   string payload;
   using (StreamReader r = new StreamReader("zwykly_rabat.json"))
   {
      payload = r.ReadToEnd();

   }

   CustomerModel strPayload = CustomerModel.FromJson(payload);
   var response = await client.PostAsync(serverUrl + "/api/Receipt", new StringContent(strPayload.ToJson(), Encoding.UTF8, "application/json"));
   var contents = await response.Content.ReadAsStringAsync();

   if (response.StatusCode == System.Net.HttpStatusCode.OK)
   {
      Console.WriteLine("-< Response (200): " + contents);
      if (string.IsNullOrWhiteSpace(contents))
      {
         Console.WriteLine("-< Warning: Received empty response from server.");
         return true;
      }
      try
      {
         DRfiscalResponse_PEPCO? re = JsonSerializer.Deserialize<DRfiscalResponse_PEPCO>(contents);
         if (re == null)
         {
            Console.WriteLine("-< Warning: Deserialized response is null.");
            return true;
         }
         Console.WriteLine("-<" + re.UID);
      }
      catch (JsonException)
      {
         Console.WriteLine("-< Error: Response is not a valid JSON.");
      }

      return true;
   }
   else
   {
      Console.WriteLine("-< Response (FAILED)");
      return false;
   }
}

//-----------------------------------------------------------------
// Main Loop
//-----------------------------------------------------------------
while (true)
{

   Thread.Sleep(10000);
   if (isConnected == true && sendMessage == false)
   {
      Console.WriteLine("-> Sending transaction to DRProxy");
      sendMessage = await ProcessRepositoriesAsync(client);

   }

}


