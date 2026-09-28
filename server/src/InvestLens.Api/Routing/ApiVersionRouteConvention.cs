using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace InvestLens.Api.Routing;

public sealed class ApiVersionRouteConvention : IApplicationModelConvention
{
    private static readonly AttributeRouteModel Prefix = new(new RouteAttribute("api/v1"));

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers.Where(controller => controller.ControllerType.Namespace == "InvestLens.Api.Controllers"))
        {
            foreach (var selector in controller.Selectors.Where(selector => selector.AttributeRouteModel is not null))
                selector.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(Prefix, selector.AttributeRouteModel);
        }
    }
}
