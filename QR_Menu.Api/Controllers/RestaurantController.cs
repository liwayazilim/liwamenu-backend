using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Restaurants;
using QR_Menu.Application.Restaurants.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace QR_Menu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RestaurantController : ControllerBase
{
    private readonly RestaurantService _service;

    public RestaurantController(RestaurantService service)
    {
        _service = service;
    }

    // GET: api/restaurant?search=...&city=...&isActive=...&page=1&pageSize=20
    [HttpGet]
    [Authorize(Roles = "Admin,Dealer,Owner")]
    public async Task<ActionResult<object>> GetAll([FromQuery] string? search, [FromQuery] string? city, [FromQuery] bool? isActive, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (restaurants, total) = await _service.GetAllAsync(search, city, isActive, page, pageSize);
        return Ok(new { total, restaurants });
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Dealer,Owner")]
    public async Task<ActionResult<RestaurantReadDto>> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Dealer,Owner")]
    public async Task<ActionResult<RestaurantReadDto>> Create(RestaurantCreateDto dto)
    {
        var created = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Dealer,Owner")]
    public async Task<IActionResult> Update(Guid id, RestaurantUpdateDto dto)
    {
        var success = await _service.UpdateAsync(id, dto);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Dealer,Owner")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _service.DeleteAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
} 