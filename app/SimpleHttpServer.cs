using System.Net;
using System.Text;
using LiteDB;
using Microsoft.Extensions.Primitives;
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
                var responseStringBuilder = new StringBuilder();
                responseStringBuilder.Append("<html><body>Hello World!</body></html>");

                Stream body = request.InputStream;
                var reader = new StreamReader(body, request.ContentEncoding);
                string jsonBody = reader.ReadToEnd();
                
                var collection = _db.GetCollection<PetWeight>("petweights");

                HttpListenerResponse response = context.Response;

                if (request.HttpMethod == "POST") {
                    AddCorsHeaders(response);
                    List<PetWeight>? petWeights = System.Text.Json.JsonSerializer.Deserialize<List<PetWeight>>(jsonBody);

                    if (petWeights != null)
                    {
                        responseStringBuilder.Append("<html><body>");

                        foreach (var petWeight in petWeights)
                        {
                            if (petWeight is not { Weight: > 0 }) continue;
                            
                            Console.WriteLine("Got a request for db insert:");
                            Console.WriteLine($"Name: {petWeight.Name}");
                            Console.WriteLine($"Weight: {petWeight.Weight}");
                            Console.WriteLine($"Date: {petWeight.Date}");
                            
                            collection.Insert(petWeight);

                            responseStringBuilder.Append(
                                $"<html><body>Inserted {petWeight.Name} to the database</body></html>");
                        }

                        responseStringBuilder.Append("</body></html>");
                    }
                } else if (request.HttpMethod == "GET") {
                    AddCorsHeaders(response);

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

                    await using var streamWriter = new StreamWriter(response.OutputStream);
                    await streamWriter.WriteAsync(responseJson);
                } else if (request.HttpMethod == "OPTIONS") {
                    Console.WriteLine("Got an OPTIONS call");
                    AddCorsHeaders(response);

                    response.StatusCode = 200;
                    response.Close();
                } else {
                    Console.WriteLine("Unhandled HTTP method!");
                    responseStringBuilder.Append("<html><body>Unhandled HTTP method!</body></html>");
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseStringBuilder.ToString());

                // Get a response stream and write the response to it.
                if (request.HttpMethod != "OPTIONS") {
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
        }

        private static void AddCorsHeaders(HttpListenerResponse response) {
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
