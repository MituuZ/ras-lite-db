using System.Net;
using System.Text;
using System.Text.Json;
using Models;
using LiteDB;

namespace HttpServer {
    public class SimpleHttpServer
    {
        string raspIp;
        LiteDatabase db;

        public async Task StartListeningAsync()
        {
            HttpListener listener = new HttpListener();

            // Add the prefixes.
            listener.Prefixes.Add($"http://{raspIp}:5000/");

            listener.Start();
            Console.WriteLine("Listening...");

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                string responseString = "<html><body>Hello World!</body></html>";

                System.IO.Stream body = request.InputStream;
                System.IO.StreamReader reader = new System.IO.StreamReader(body, request.ContentEncoding);
                string jsonBody = reader.ReadToEnd();
                
                var collection = db.GetCollection<PetWeight>("petweights");

                HttpListenerResponse response = context.Response;

                if (request.HttpMethod == "POST") {
                    AddCORSHeaders(response);
                    List<PetWeight>? petWeights = System.Text.Json.JsonSerializer.Deserialize<List<PetWeight>>(jsonBody);

                    if (petWeights != null) {
                        responseString = "<html><body>";

                        foreach (var petWeight in petWeights) {
                            if (petWeight != null) {
                                Console.WriteLine($"Got a request for db insert:");
                                Console.WriteLine($"Name: {petWeight.Name}");
                                Console.WriteLine($"Weight: {petWeight.Weight}");
                                Console.WriteLine($"Date: {petWeight.Date}");
                                
                                collection.Insert(petWeight);

                                responseString += $"<html><body>Inserted {petWeight.Name} to the database</body></html>";
                            }
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

                    var responseJson = System.Text.Json.JsonSerializer.Serialize(petWeights);
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
                    response.ContentLength64 = buffer.Length;
                    System.IO.Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);

                    // You must close the output stream.
                    output.Close();
                }
            }
        }

        private void AddCORSHeaders(HttpListenerResponse response) {
            response.AddHeader("Access-Control-Allow-Origin", "*");
            response.AddHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
            response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept");
        }

        public SimpleHttpServer(string _raspIp, LiteDatabase _db) {
            this.raspIp = _raspIp;
            this.db = _db;
        }
    }
}
