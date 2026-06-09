using System.ComponentModel.DataAnnotations;

namespace visitors_mangement_system.Models
{
    public class Visitor
    {
        public int VisitorId { get; set; }

        public string Name { get; set; } = string.Empty;

      
        public string PhoneNumber { get; set; } = string.Empty;

        public DateTime BirthDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime? UpdatedDate { get; set; }

        public string? UpdatedBy { get; set; }

        public DateTime? DeletedDate { get; set; }

        public string? DeletedBy { get; set; }
    }
}