using Lesson_7.DTOs;
using Lesson_7.Services;
using Microsoft.AspNetCore.Mvc;

namespace Lesson_7.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehouseController(IDbService db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> GetWarehouseId(GetWareHouseDTO wareHouseDto)
    {
        int id;
        try
        {
            id = await db.GetIdProductWarehouse(wareHouseDto);
        }
        catch (ArgumentException e)
        {
            return NotFound(e.Message);
        }
        return Ok(id);
    }
}