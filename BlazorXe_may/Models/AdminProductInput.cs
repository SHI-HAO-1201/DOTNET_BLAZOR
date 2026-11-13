using System.ComponentModel.DataAnnotations;
using MyMvcApp.Data;

namespace MyMvcApp.Models;

public sealed class AdminProductInput
{
    public int Id { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Brand { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Type { get; set; } = string.Empty;

    [Range(0, long.MaxValue)]
    public long Price { get; set; }

    [Required, StringLength(40)]
    public string Engine { get; set; } = string.Empty;

    [Required, StringLength(60)]
    public string Power { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Gear { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Image { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Code { get; set; } = string.Empty;

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    [Required]
    public string AvailabilityStatus { get; set; } = MotorcycleAvailabilityStatus.ForSale;
}
