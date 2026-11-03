using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TuTuc.Models;
using System.Security.Claims;

namespace TuTuc.Services;

// 4.9.1: business failures keep the current page and return the specified HTTP status.
public sealed class BusinessErrorFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();
        if (executed.Exception is not BusinessFault error) return;
        executed.ExceptionHandled = true;
        var response=context.HttpContext.Response;
        response.StatusCode=error.StatusCode;
        response.Headers["X-Business-Error"]=error.Code;
        if(error.StatusCode is 403 or 404){executed.Result=new StatusCodeResult(error.StatusCode);return;}
        if(context.Controller is not Controller controller){executed.Result=new ObjectResult(error.Message){StatusCode=error.StatusCode};return;}
        controller.ModelState.AddModelError("",error.Message);
        controller.ViewData["BusinessErrorCode"]=error.Code;
        var user=int.Parse(context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var provider=context.HttpContext.User.IsInRole("Provider");
        var id=context.ActionArguments.TryGetValue("id",out var argument)&&argument is int number?number:(int?)null;
        var manager=context.HttpContext.RequestServices.GetRequiredService<MarketplaceManager>();
        var model=await manager.Requests(user,provider,id);
        model.DraftBookingId=id;
        if(context.ActionArguments.TryGetValue("terms",out var terms)&&terms is BookingTermsInput input)model.DraftTerms=input;
        if(context.ActionArguments.TryGetValue("reason",out var reason))model.DraftReason=reason as string;
        if(context.ActionArguments.TryGetValue("decision",out var decision))model.DraftDecision=decision as string;
        executed.Result=controller.View("~/Views/Marketplace/Requests.cshtml",model);
    }
}
