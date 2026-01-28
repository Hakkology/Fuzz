using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Fuzz.Domain.Entities;

[Table("FuzzUsers")]
public class FuzzUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
