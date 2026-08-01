using System;
using System.Linq;
using Ecommerce.Data;
using Ecommerce.Data.Repositories;

namespace Ecommerce.Data.SmokeTest
{
    /// <summary>
    /// Spec 03 acceptance: Add → Save → GetById round-trip against SQL Express.
    /// </summary>
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                // Ensure EF reads connection string from this app's config.
                using (var context = new EcommerceEntities())
                {
                    if (!context.Database.Exists())
                    {
                        Console.WriteLine("FAIL: LegacyEcommerceDb does not exist on .\\SQLEXPRESS");
                        return 1;
                    }

                    var categoryId = context.Categories.Select(c => c.CategoryId).FirstOrDefault();
                    if (categoryId == 0)
                    {
                        Console.WriteLine("FAIL: No categories seeded — run database/01_SeedData.sql first");
                        return 1;
                    }

                    var repo = new ProductRepository(context);
                    var marker = "Spec03-Smoke-" + Guid.NewGuid().ToString("N").Substring(0, 8);

                    var created = new Product
                    {
                        CategoryId = categoryId,
                        Name = marker,
                        Description = "Temporary Spec 03 round-trip product",
                        Price = 1.23m,
                        ThumbnailUrl = null,
                        Stock = 7,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };

                    repo.Add(created);
                    repo.Save();

                    var newId = created.ProductId;
                    if (newId <= 0)
                    {
                        Console.WriteLine("FAIL: ProductId was not generated after Save");
                        return 1;
                    }

                    // New context to prove persistence (not just tracked instance)
                    using (var verifyContext = new EcommerceEntities())
                    {
                        var verifyRepo = new Repository<Product>(verifyContext);
                        var loaded = verifyRepo.GetById(newId);

                        if (loaded == null)
                        {
                            Console.WriteLine("FAIL: GetById returned null for id " + newId);
                            return 1;
                        }

                        if (loaded.Name != marker || loaded.Stock != 7 || loaded.Price != 1.23m)
                        {
                            Console.WriteLine("FAIL: Loaded product did not match saved values");
                            Console.WriteLine("  Name={0}, Stock={1}, Price={2}", loaded.Name, loaded.Stock, loaded.Price);
                            return 1;
                        }

                        // Specialized query smoke: detail with includes
                        var detailRepo = new ProductRepository(verifyContext);
                        var detail = detailRepo.GetByIdWithDetails(newId);
                        if (detail == null || detail.Category == null)
                        {
                            Console.WriteLine("FAIL: GetByIdWithDetails did not load product/category");
                            return 1;
                        }

                        // Cleanup
                        verifyRepo.Delete(loaded);
                        verifyRepo.Save();

                        if (verifyRepo.GetById(newId) != null)
                        {
                            Console.WriteLine("FAIL: Cleanup delete did not remove product " + newId);
                            return 1;
                        }
                    }

                    Console.WriteLine("PASS: Add → Save → GetById round-trip OK (id {0})", newId);
                    Console.WriteLine("PASS: GetByIdWithDetails eager-load OK");
                    return 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAIL: " + ex);
                return 1;
            }
        }
    }
}
