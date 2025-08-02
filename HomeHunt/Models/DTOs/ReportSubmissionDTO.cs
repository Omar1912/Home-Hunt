using System.ComponentModel.DataAnnotations;

namespace HomeHunt.Models.DTOs
{
    public class ReportSubmissionDTO
    {
        [Required]
        public int PropertyId { get; set; }
    }
}