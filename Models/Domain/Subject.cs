using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolEduERP.Models.Domain;

public class Subject : AuditableEntity
{
    [Key]
    public int Id { get; set; }

    // Standard: Indian style numeric (1..12)
    [Range(1, 12)]
    public int Standard { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Code { get; set; }

    public int? TeacherId { get; set; }
    [ForeignKey(nameof(TeacherId))]
    public Teacher? Teacher { get; set; }
}
