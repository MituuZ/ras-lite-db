using System.Net;
using System.Text;
using LiteDB;
using Models;

namespace RasLiteDB {
    public class SimpleHttpServer
    {
        private readonly string _raspIp;
        private readonly LiteDatabase _db;

        public async Task StartListeningAsync()
        {
            var listener = new HttpListener();

            // Add the prefixes.
            listener.Prefixes.Add($"http://{_raspIp}:5000/");

            listener.Start();
            Console.WriteLine("Listening...");

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                var responseString = "<html><body>Hello World!</body></html>";

                Stream body = request.InputStream;
                var reader = new StreamReader(body, request.ContentEncoding);
                string jsonBody = reader.ReadToEnd();
                
                var collection = _db.GetCollection<PetWeight>("petweights");

                HttpListenerResponse response = context.Response;

                if (request.HttpMethod == "POST") {
                    AddCORSHeaders(response);
                    List<PetWeight>? petWeights = System.Text.Json.JsonSerializer.Deserialize<List<PetWeight>>(jsonBody);

                    if (petWeights != null) {
                        responseString = "<html><body>";

                        foreach (var petWeight in petWeights)
                        {
                            if (petWeight is not { Weight: > 0 }) continue;
                            
                            Console.WriteLine("Got a request for db insert:");
                            Console.WriteLine($"Name: {petWeight.Name}");
                            Console.WriteLine($"Weight: {petWeight.Weight}");
                            Console.WriteLine($"Date: {petWeight.Date}");
                            
                            collection.Insert(petWeight);

                            responseString += $"<html><body>Inserted {petWeight.Name} to the database</body></html>";
                        }

                        responseString += "</body></html>";
                    }
                } else if (request.HttpMethod == "GET") {
                    AddCORSHeaders(response);

                    var allItems = collection.FindAll();
                    var petWeights = new List<PetWeight>();

                    foreach (PetWeight item in allItems)
                    {
                        var petWeight = new PetWeight(item.Name, item.Weight, item.Date);

                        petWeights.Add(petWeight);
                    }

                    string responseJson = System.Text.Json.JsonSerializer.Serialize(petWeights);
                    response.ContentType = "application/json";
                    response.StatusCode = 200;
                    
                    using (var streamWriter = new StreamWriter(response.OutputStream))
                    {
                        streamWriter.Write(responseJson);
                    }
                } else if (request.HttpMethod == "OPTIONS") {
                    Console.WriteLine("Got an OPTIONS call");
                    AddCORSHeaders(response);

                    response.StatusCode = 200;
                    response.Close();
                } else {
                    Console.WriteLine("Unhandled HTTP method!");
                    responseString = "<html><body>Unhandled HTTP method!</body></html>";
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                // Get a response stream and write the response to it.
                if (request.HttpMethod != "OPTIONS") {
                    try {
                        response.ContentLength64 = buffer.Length;
                        System.IO.Stream output = response.OutputStream;
                        output.Write(buffer, 0, buffer.Length);

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
        }

        private void AddCORSHeaders(HttpListenerResponse response) {
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept");
        }

        public SimpleHttpServer(string raspIp, LiteDatabase db) {
            _raspIp = raspIp;
            _db = db;
        }
    }
}
