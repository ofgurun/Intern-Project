namespace InternProject.Models
{
    public class DropdownOption
    {
        public int Id { get; set; }
        public string? Type { get; set; }      // Örn: "Başvuran Birim"
        public string? Subtype { get; set; }   // Örn: "Bilgi İşlem"
        public bool Delete { get; set; }
    }

}
