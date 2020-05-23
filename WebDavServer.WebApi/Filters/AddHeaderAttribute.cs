using Microsoft.AspNetCore.Mvc.Filters;
using System;

namespace WebDavServer.WebApi.Filters
{
    public class AddHeaderAttribute : Attribute, IActionFilter
    {
        private readonly string _name;
        private readonly string _value;

        public AddHeaderAttribute(string name, string value)
        {
            _name = name;
            _value = value;
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            context.HttpContext.Response.Headers.Add(_name, new string[] { _value });
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {

        }
    }
}
