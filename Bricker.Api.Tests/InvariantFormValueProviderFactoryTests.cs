using System.Globalization;
using Bricker.Api.Binding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Primitives;

namespace Bricker.Api.Tests;

public sealed class InvariantFormValueProviderFactoryTests
{
    [Fact]
    public async Task Form_price_with_dot_is_parsed_as_decimal_even_under_pt_br()
    {
        var request = new DefaultHttpContext().Request;
        request.ContentType = "multipart/form-data; boundary=test";
        request.Form = new FormCollection(new Dictionary<string, StringValues> { ["price"] = "12.50" });
        var actionContext = new ActionContext { HttpContext = request.HttpContext };
        var context = new ValueProviderFactoryContext(actionContext);

        await new InvariantFormValueProviderFactory().CreateValueProviderAsync(context);

        var result = context.ValueProviders.Single().GetValue("price");
        Assert.Equal(CultureInfo.InvariantCulture, result.Culture);
        Assert.Equal(12.50m, decimal.Parse(result.FirstValue!, NumberStyles.Number, result.Culture));
    }
}
