using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sh8lny.Abstraction.Services;
using Sh8lny.Shared.DTOs.Announcements;

namespace Sh8lny.Web.Controllers;

/// <summary>
/// Controller for platform-wide announcements.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AnnouncementsController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;

    public AnnouncementsController(IAnnouncementService announcementService)
    {
        _announcementService = announcementService;
    }

    /// <summary>
    /// Returns all announcements ordered by newest first.
    /// Public endpoint — used by the mobile home screen.
    /// </summary>
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<List<AnnouncementDto>>> GetAnnouncements()
    {
        var result = await _announcementService.GetAnnouncementsAsync();

        if (!result.IsSuccess)
            return StatusCode(500, result);

        return Ok(result.Data);
    }

    /// <summary>
    /// Creates a new announcement. Admin only.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<AnnouncementDto>> CreateAnnouncement([FromBody] CreateAnnouncementDto dto)
    {
        var result = await _announcementService.CreateAsync(dto);

        if (!result.IsSuccess)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetAnnouncements), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Updates an existing announcement. Admin only.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<AnnouncementDto>> UpdateAnnouncement(int id, [FromBody] CreateAnnouncementDto dto)
    {
        var result = await _announcementService.UpdateAsync(id, dto);

        if (!result.IsSuccess)
        {
            if (result.Message == "Announcement not found.")
                return NotFound(result);

            return BadRequest(result);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Deletes an announcement. Admin only.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAnnouncement(int id)
    {
        var result = await _announcementService.DeleteAsync(id);

        if (!result.IsSuccess)
        {
            if (result.Message == "Announcement not found.")
                return NotFound(result);

            return BadRequest(result);
        }

        return NoContent();
    }
}
