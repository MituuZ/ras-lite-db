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
                    var allItems = collection.FindAll();
                    responseString = "<html><body>";

                    foreach (PetWeight item in allItems)
                    {
                        var petString = $"{item.Name} weighs {item.Weight} on date {item.Date}";
                        Console.WriteLine(petString);
                        responseString += petString;
                        responseString += "\n";
                    }

                    responseString += "</body></html>";
                } else if (request.HttpMethod == "OPTIONS") {
                    Console.WriteLine("Got an OPTIONS call");
                    response.AddHeader("Access-Control-Allow-Origin", "*");
                    response.AddHeader("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
                    response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept");
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

        public SimpleHttpServer(string _raspIp, LiteDatabase _db) {
            this.raspIp = _raspIp;
            this.db = _db;
        }
    }
}