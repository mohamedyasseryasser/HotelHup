using HotelHup.APPLICATION.DTO.General;
using System.ComponentModel.DataAnnotations;

namespace HotelHup.APPLICATION.DTO.services;

public sealed class CreateServiceRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }

    [StringLength(100)]
    public string? RevenueCategory { get; init; }

    [Range(typeof(decimal), "0.01", "99999999999999.99")]
    public decimal Price { get; init; }

    [Range(typeof(decimal), "0", "99999999999999.99")]
    public decimal Tax { get; init; }
}

public sealed class UpdateServiceRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; init; }

    [StringLength(100)]
    public string? RevenueCategory { get; init; }

    [Range(typeof(decimal), "0.01", "99999999999999.99")]
    public decimal Price { get; init; }

    [Range(typeof(decimal), "0", "99999999999999.99")]
    public decimal Tax { get; init; }
}

public sealed class DeactivateServiceRequest
{
    [StringLength(500)]
    public string? Reason { get; init; }
}

public sealed class ServiceListRequest : pagination
{
    public int? PropertyId { get; init; }
    public bool IncludeInactive { get; init; }

    [StringLength(150)]
    public string? Search { get; init; }
}

public sealed class ServiceResponse
{
    public int Id { get; init; }
    public int PropertyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? RevenueCategory { get; init; }
    public decimal Price { get; init; }
    public decimal Tax { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }
}
