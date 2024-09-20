using Microsoft.AspNetCore.Mvc;
using PiBot.Telegram.Filters;
using PiBot.Telegram.Services;
using Telegram.Bot.Types;

namespace PiBot.Telegram.Controllers;

[ApiController]
[Route("/api/tg-webhook")]
public class WebhookController : ControllerBase
{
    [HttpPost]
    [ValidateTelegramBot]
    public async Task<IActionResult> Update(
        [FromBody] Update update,
        [FromServices] UpdateHandlers handleUpdateService,
        CancellationToken cancellationToken)
    {
        await handleUpdateService.HandleUpdateAsync(update, cancellationToken);
        return Ok();
    }
}
