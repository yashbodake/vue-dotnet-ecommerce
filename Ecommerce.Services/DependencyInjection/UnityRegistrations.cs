using Ecommerce.Core.Interfaces;
using Ecommerce.Data;
using Ecommerce.Data.Repositories;
using Unity;
using Unity.Lifetime;

namespace Ecommerce.Services.DependencyInjection
{
    /// <summary>
    /// Composition root helper so Ecommerce.Web never references Ecommerce.Data directly.
    /// </summary>
    public static class UnityRegistrations
    {
        public static void Register(IUnityContainer container)
        {
            container.RegisterType<EcommerceEntities>(new HierarchicalLifetimeManager());
            container.RegisterType(typeof(IRepository<>), typeof(Repository<>));
            container.RegisterType<IProductRepository, ProductRepository>();
            container.RegisterType<IProductService, ProductService>();
            container.RegisterType<ICartService, CartService>();
            container.RegisterType<IOrderService, OrderService>();
        }
    }
}
