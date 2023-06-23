using System.Net;
using System.Text;
using System.Text.Json;
using Models;

namespace HttpServer {
    public class SimpleHttpServer
    {
        string raspIp;

        public async Task StartListeningAsync()
        {
            HttpListener listener = new HttpListener();

            // Add the prefixes.
            listener.Prefixes.Add($"http://{raspIp}:5000/");

            listener.Start();
            Console.WriteLine("Listening...");

            while (true)
            {
                // Note: The GetContextAsync method blocks while waiting for a request. 
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                string responseString = "<html><body>Hello World!</body></html>";

                System.IO.Stream body = request.InputStream;
                System.IO.StreamReader reader = new System.IO.StreamReader(body, request.ContentEncoding);
                string jsonBody = reader.ReadToEnd();

                PetWeight? obj = JsonSerializer.Deserialize<PetWeight>(jsonBody);

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);

                // Get a response stream and write the response to it.
                HttpListenerResponse response = context.Response;
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);

                if (obj != null) {
                    Console.WriteLine($"Got a request: {obj.Name}");
                }

                // You must close the output stream.
                output.Close();
            }
        }

        public SimpleHttpServer(string _raspIp) {
            this.raspIp = _raspIp;
        }
    }
}