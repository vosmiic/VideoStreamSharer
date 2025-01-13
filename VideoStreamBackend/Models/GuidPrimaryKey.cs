using System.ComponentModel.DataAnnotations;

namespace VideoStreamBackend.Models;

public class GuidPrimaryKey {
    [Key]
    public Guid Id { get; set; }
}