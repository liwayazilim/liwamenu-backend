using Microsoft.AspNetCore.Mvc;
using QR_Menu.Application.Common;

namespace QR_Menu.Api.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected async Task<ActionResult<object>> GetPaginatedDataAsync<T>(
        Func<int, int, Task<(List<T> Data, int TotalCount)>> dataProvider,
        int? pageNumber,
        int? pageSize,
        string successMessageTR,
        string successMessageEN,
        string notFoundMessageTR = "Veri bulunamadı",
        string notFoundMessageEN = "Data not found")
    {
        var response = await PaginationHelper.CreatePaginatedResponseAsync(
            dataProvider, pageNumber, pageSize, successMessageTR, successMessageEN, notFoundMessageTR, notFoundMessageEN);

        // If response is ResponsBase (when both parameters are null), handle it accordingly
        if (response is ResponsBase responsBase)
        {
            return responsBase.StatusCode == "404" ? NotFound(responsBase) : Ok(responsBase);
        }
        
        // If response is data object (when pagination parameters are provided), return it directly
        return Ok(response);
    }

    protected ActionResult<ResponsBase> Success<T>(T data, string messageTR, string messageEN)
    {
        return Ok(ResponsBase.Create(messageTR, messageEN, "200", data));
    }

    protected ActionResult<ResponsBase> NotFound(string messageTR, string messageEN)
    {
        return NotFound(ResponsBase.Create(messageTR, messageEN, "404"));
    }

    protected ActionResult<ResponsBase> BadRequest(string messageTR, string messageEN)
    {
        return BadRequest(ResponsBase.Create(messageTR, messageEN, "400"));
    }

    protected ActionResult<ResponsBase> Unauthorized(string messageTR, string messageEN)
    {
        return Unauthorized(ResponsBase.Create(messageTR, messageEN, "401"));
    }
} 