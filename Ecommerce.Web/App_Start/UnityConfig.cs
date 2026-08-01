using System.Web.Mvc;
using Ecommerce.Services.DependencyInjection;
using Unity;
using Unity.Mvc5;

namespace Ecommerce.Web
{
    public static class UnityConfig
    {
        public static void RegisterComponents()
        {
            var container = new UnityContainer();
            UnityRegistrations.Register(container);
            DependencyResolver.SetResolver(new UnityDependencyResolver(container));
        }
    }
}
