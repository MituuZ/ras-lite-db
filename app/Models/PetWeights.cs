namespace RasLiteDB.Models {
    internal class PetWeight {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Weight { get; set; }
        public DateTime Date { get; set; }

        public PetWeight(string name, double weight, DateTime date) {
            Name = name;
            Weight = weight;
            Date = date;
        }
    }
}
