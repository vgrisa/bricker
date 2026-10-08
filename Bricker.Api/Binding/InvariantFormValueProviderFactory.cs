using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Bricker.Api.Binding;

/// <summary>
/// FormData enviado por navegadores usa ponto como separador decimal. Mantemos
/// esse contrato independente da cultura do servidor (por exemplo, pt-BR).
/// </summary>
public sealed class InvariantFormValueProviderFactory : IValueProviderFactory
{
    public async Task CreateValueProviderAsync(ValueProviderFactoryContext context)
    {
        var request = context.ActionContext.HttpContext.Request;
        if (!request.HasFormContentType) return;

        var form = await request.ReadFormAsync();
        context.ValueProviders.Add(new FormValueProvider(BindingSource.Form, form, CultureInfo.InvariantCulture));
    }
}
