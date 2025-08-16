using System.ComponentModel.DataAnnotations;

namespace QR_Menu.Application.Restaurants.DTOs;

public class WorkingHoursDayDto
{
	// 1 = Monday ... 7 = Sunday
	[Range(1, 7)]
	public int Day { get; set; }
	public bool IsClosed { get; set; }
	// HH:mm (24h)
	public string? Open { get; set; }
	public string? Close { get; set; }
}

public class WorkingHoursReadDto
{
	public Guid RestaurantId { get; set; }
	public List<WorkingHoursDayDto> Days { get; set; } = new();
}

public class WorkingHoursUpdateDto
{
	[Required]
	public Guid RestaurantId { get; set; }
	[Required]
	public List<WorkingHoursDayDto> Days { get; set; } = new();
} 