using System.ComponentModel.DataAnnotations;

namespace FinOS.Identity.Application.DTOs;

public class RoleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AssignRoleRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public long UserId { get; set; }

    [Required(ErrorMessage = "Role ID is required")]
    public int RoleId { get; set; }
}
