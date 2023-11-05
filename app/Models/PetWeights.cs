namespace Models {
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