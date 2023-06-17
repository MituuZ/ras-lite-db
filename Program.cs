using System;
using LiteDB;

namespace Application {
    class Program {
        public static void Main(string[] args) {
            Console.WriteLine("Hello, World!");

            # Creates the database if it doesn't exist
            using(var db = new LiteDatabase(@"/home/mituuz/.liteDb")) {
                
                # Creates the collection if it doesn't exist
                var collection = db.GetCollection<PetWeight>("petweights");

                var petWeight = new PetWeight (
                    "Pet",
                    1.5,
                    DateTime.Now
                );

                collection.Insert(petWeight);

                collection.EnsureIndex(x => x.Name);

                var results = collection.Query()
                    .Where(x => x.Name.StartsWith("P"))
                    .ToList();

                foreach (var weight in results)
                {
                    Console.WriteLine(weight.Name);
                }
            }
        }
    }

    class PetWeight {
        public int Id { get; set; }
        public String Name { get; set; }
        public Double Weight { get; set; }
        public DateTime Date { get; set; }

        public PetWeight(String Name, Double Weight, DateTime Date) {
            this.Name = Name;
            this.Weight = Weight;
            this.Date = Date;
        }
    }
}
