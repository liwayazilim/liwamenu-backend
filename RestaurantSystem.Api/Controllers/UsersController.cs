using Microsoft.AspNetCore.Mvc;
using RestaurantSystem.Application.Users;
using Microsoft.AspNetCore.Authorization;
using RestaurantSystem.Application.Users.DTOs;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RestaurantSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    // GET: api/users?search=...&page=1&pageSize=20
    [HttpGet]
    [Authorize(Roles = "Admin,Dealer")] // Only admin/dealer can list users
    public async Task<ActionResult<object>> GetAll([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var (users, total) = await _userService.GetAllAsync(search, page, pageSize);
        return Ok(new { total, users });
    }

    // GET: api/users/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Dealer")] // Only admin/dealer can get user details
    public async Task<ActionResult<UserReadDto>> GetById(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    // POST: api/users
    [HttpPost]
    [Authorize(Roles = "Admin,Dealer")] // Only admin/dealer can add users
    public async Task<ActionResult<UserReadDto>> Create([FromBody] UserCreateDto dto)
    {
        var user = await _userService.CreateAsync(dto);
        if (user == null) return BadRequest(new { message = "Email already exists." });
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    // PUT: api/users/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Dealer")] // Only admin/dealer can update users
    public async Task<IActionResult> Update(Guid id, [FromBody] UserUpdateDto dto)
    {
        var success = await _userService.UpdateAsync(id, dto);
        if (!success) return NotFound();
        return NoContent();
    }

    // DELETE: api/users/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Dealer")] // Only admin/dealer can delete users
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _userService.DeleteAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }
} 