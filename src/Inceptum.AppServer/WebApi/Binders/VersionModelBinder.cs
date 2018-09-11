using System;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
using System.Web.Http.ValueProviders;

namespace Inceptum.AppServer.WebApi.Binders
{
    public class VersionModelBinder : IModelBinder
    {
        public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType != typeof(Version))
            {
                return false;
            }
            ValueProviderResult val = bindingContext.ValueProvider.GetValue(
                bindingContext.ModelName);
            if (val == null)
            {
                return false;
            }
            var value = val.RawValue as string;
            if (value == null)
            {
                bindingContext.ModelState.AddModelError(bindingContext.ModelName, "Wrong value type");
                return false;
            }


            Version version;
            if (Version.TryParse(value, out version))
            {
                bindingContext.Model = version;
                return true;
            }

            bindingContext.ModelState.AddModelError(
                bindingContext.ModelName, "Cannot convert value to Vresion");
            return false;
        }
    }
}