using System.ComponentModel.DataAnnotations;

namespace EShop.ApiService.Contracts;

/// <summary>Request data for creating a product.</summary>
public sealed record CreateProductRequest
{
    [Required]
    public required string Name { get; init; }

    public string? Description { get; init; }

    public string SKU { get; init; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public required decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public required int StockQuantity { get; init; }

    public string Category { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}

/// <summary>Request data for replacing an existing product.</summary>
public sealed record UpdateProductRequest
{
    [Required]
    public required string Name { get; init; }

    public string? Description { get; init; }

    public string SKU { get; init; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public required decimal Price { get; init; }

    [Range(0, int.MaxValue)]
    public required int StockQuantity { get; init; }

    public string Category { get; init; } = string.Empty;

    public required bool IsActive { get; init; }
}

/// <summary>Product details returned by the API.</summary>
public sealed record ProductResponse(
    Guid Id,
    string Name,
    string? Description,
    string SKU,
    decimal Price,
    int StockQuantity,
    string Category,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
