using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Chinnaphat_8272_L005.Models;

[Table("labstudent")]
public class Labstudent
{
    [Key]
    [Required]
    public string StdId { get; set; } = null!;

    [Required]
    public string StdPassword { get; set; } = null!;

    [Required]
    public string StdName { get; set; } = null!;

    [Required]
    public string StdLastname { get; set; } = null!;
}