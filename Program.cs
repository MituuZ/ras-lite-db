using System;
using System.Net;
using LiteDB;
using HttpServer;
using Models;
using Microsoft.Extensions.Configuration;

namespace Application {
    class Program {
        public static async Task Main(string[] args) {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appsettings.json");
            configurationBuilder.AddJsonFile("appsettings.Development.json", optional: true);
            var configuration = configurationBuilder.Build();

            string? dbPath = configuration["RasLiteSettings:DbPath"];
            string? raspIp = configuration["RasLiteSettings:RaspIp"];

            if (raspIp == null) {
                Console.WriteLine("No port defined in appsettings.json!");
                return;
            }
            SimpleHttpServer server = new SimpleHttpServer(raspIp);

            await server.StartListeningAsync();

            // Creates the database if it doesn't exist
            using(var db = new LiteDatabase(dbPath)) {
                
                // Creates the collection if it doesn't exist
                var collection = db.GetCollection<PetWeight>("petweights");
            }
        }
    }
}
