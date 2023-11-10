using System.Net;
using System.Text;
using LiteDB;
using RasLiteDB.Models;

namespace RasLiteDB {
    public class SimpleHttpServer
    {
        private readonly string _raspIp;
        private readonly LiteDatabase _db;
        private bool _running;

        public SimpleHttpServer(string raspIp, LiteDatabase db) {
            _raspIp = raspIp;
            _db = db;
            _running = true;
        }

        public async Task StartListeningAsync()
        {
            var listener = new HttpListener();

            // Add the prefixes.
            listener.Prefixes.Add($"http://{_raspIp}:5000/");

            listener.Start();
            Console.WriteLine("Listening...");

            while (_running)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                var responseStringBuilder = new StringBuilder();
                Stream body = request.InputStream;
                var reader = new StreamReader(body, request.ContentEncoding);
                string jsonBody = await reader.ReadToEndAsync();
                
                var collection = _db.GetCollection<PetWeight>("petweights");
                HttpListenerResponse response = context.Response;

                switch (request.HttpMethod)
                {
                    case "POST":
                        if (context.Request.Url != null && context.Request.Url.AbsolutePath == "/stop")
                        {
                            _running = false;
                            Console.WriteLine("Shutting down the service.");
                            responseStringBuilder.Append("Shutting down the service");
                            response.StatusCode = 200;
                        }
                        else
                        {
                            HandlePostRequest(response, collection, jsonBody, responseStringBuilder);
                        }
                        break;
                    case "GET":
                        await HandleGetRequest(response, collection);
                        break;
                    case "OPTIONS":
                        HandleOptionsRequest(response);
                        break;
                    default:
                        Console.WriteLine("Unhandled HTTP method!");
                        responseStringBuilder.Append("Unhandled HTTP method!");
                        response.StatusCode = 400;
                        break;
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseStringBuilder.ToString());

                // Get a response stream and write the response to it.
                if (request.HttpMethod == "OPTIONS") continue;
                
                try {
                    response.ContentLength64 = buffer.Length;
                    Stream output = response.OutputStream;
                    await output.WriteAsync(buffer);

                    // You must close the output stream.
                    output.Close();
                    Console.WriteLine("Writing a response out");
                } catch (ObjectDisposedException) {
                    Console.WriteLine("Failed to write a response!");
                } catch (InvalidOperationException) {
                    Console.WriteLine("Failed to write a response!");
                }
            }
        }

        private static async Task HandleGetRequest(HttpListenerResponse response, ILiteCollection<PetWeight> collection)
        {
            Console.WriteLine("Got a get request, returning PetWeights");
            AddCorsHeaders(response);

            var allItems = collection.FindAll();
            var petWeights = allItems.Select(item => new PetWeight(item.Name, item.Weight, item.Date)).ToList();

            string responseJson = System.Text.Json.JsonSerializer.Serialize(petWeights);
            response.ContentType = "application/json";
            response.StatusCode = 201;

            await using var streamWriter = new StreamWriter(response.OutputStream);
            await streamWriter.WriteAsync(responseJson);
        }
        
        private static void HandleOptionsRequest(HttpListenerResponse response)
        {
            Console.WriteLine("Got an OPTIONS call");
            AddCorsHeaders(response);

            response.StatusCode = 200;
            response.Close();
        }

        private static void HandlePostRequest(HttpListenerResponse response, ILiteCollection<PetWeight> collection, string jsonBody, StringBuilder responseStringBuilder)
        {
            AddCorsHeaders(response);
            List<PetWeight>? petWeights = null;
            try
            {
                petWeights = System.Text.Json.JsonSerializer.Deserialize<List<PetWeight>>(jsonBody);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to parse a JSON message: {e.Message}");
                responseStringBuilder.Append("Failed to parse JSON");
                response.StatusCode = 500;
            }

            if (petWeights == null) return;
            
            foreach (PetWeight petWeight in petWeights.Where(petWeight => petWeight is { Weight: > 0 }))
            {
                Console.WriteLine("Got a request for db insert:");
                Console.WriteLine($"Name: {petWeight.Name}");
                Console.WriteLine($"Weight: {petWeight.Weight}");
                Console.WriteLine($"Date: {petWeight.Date}");
                
                collection.Insert(petWeight);
                responseStringBuilder.Append($"Inserted {petWeight.Name} to the database");
            }
        }

        private static void AddCorsHeaders(HttpListenerResponse response) {
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept");
        }
    }
}
