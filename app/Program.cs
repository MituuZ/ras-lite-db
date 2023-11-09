using HttpServer;
using LiteDB;
using Microsoft.Extensions.Configuration;

namespace RasLiteDB {
    internal static class Program {
        public static async Task Main(string[] args) {
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddJsonFile("appSettings.json");
            configurationBuilder.AddJsonFile("appSettings.Development.json", optional: true);
            IConfigurationRoot configuration = configurationBuilder.Build();

            string? dbPath = configuration["RasLiteSettings:DbPath"];
            string? raspIp = configuration["RasLiteSettings:RaspIp"];

            if (raspIp == null) {
                Console.WriteLine("No IP defined in appSettings.json!");
                return;
            }

            // Creates the database if it doesn't exist
            using var db = new LiteDatabase(dbPath);
            var server = new SimpleHttpServer(raspIp, db);

            await server.StartListeningAsync();
        }
    }
}
