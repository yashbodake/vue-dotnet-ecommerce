using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Ecommerce.Core.ViewModels;
using Ecommerce.Data;
using Ecommerce.Data.Repositories;
using Ecommerce.Services;

namespace Ecommerce.Services.SmokeTest
{
    /// <summary>
    /// Spec 04 acceptance:
    /// 1) Insufficient stock throws and rolls back (no order, stock unchanged)
    /// 2) Happy path decrements stock and creates OrderItem rows
    /// </summary>
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                using (var context = new EcommerceEntities())
                {
                    var productRepo = new ProductRepository(context);
                    var orderRepo = new Repository<Order>(context);
                    var orderService = new OrderService(context, productRepo, orderRepo);

                    var product = context.Products.FirstOrDefault(p => p.IsActive && p.Stock > 0);
                    if (product == null)
                    {
                        Console.WriteLine("FAIL: Need at least one in-stock product (run seed script)");
                        return 1;
                    }

                    var productId = product.ProductId;
                    var originalStock = product.Stock;
                    var userId = "spec04-smoke-user";

                    // --- Test 1: insufficient stock rolls back ---
                    var badCart = new CartViewModel
                    {
                        Lines = new List<CartLineViewModel>
                        {
                            new CartLineViewModel
                            {
                                ProductId = productId,
                                ProductName = product.Name,
                                UnitPrice = product.Price,
                                Quantity = originalStock + 100
                            }
                        }
                    };

                    var orderCountBefore = context.Orders.Count(o => o.UserId == userId);
                    var threw = false;
                    try
                    {
                        orderService.CreateOrder(userId, "123 Test St", badCart);
                    }
                    catch (InvalidOperationException ex) when (ex.Message.IndexOf("Insufficient stock", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        threw = true;
                        Console.WriteLine("OK: insufficient stock threw — " + ex.Message);
                    }

                    if (!threw)
                    {
                        Console.WriteLine("FAIL: expected InvalidOperationException for insufficient stock");
                        return 1;
                    }

                    // Reload from DB
                    context.Entry(product).Reload();
                    if (product.Stock != originalStock)
                    {
                        Console.WriteLine("FAIL: stock changed after rollback. Was {0}, now {1}", originalStock, product.Stock);
                        return 1;
                    }

                    var orderCountAfterFail = context.Orders.Count(o => o.UserId == userId);
                    if (orderCountAfterFail != orderCountBefore)
                    {
                        Console.WriteLine("FAIL: order row created despite insufficient stock");
                        return 1;
                    }

                    Console.WriteLine("PASS: insufficient-stock scenario rolled back with zero side effects");

                    // --- Test 2: happy path ---
                    var buyQty = 1;
                    var goodCart = new CartViewModel
                    {
                        Lines = new List<CartLineViewModel>
                        {
                            new CartLineViewModel
                            {
                                ProductId = productId,
                                ProductName = product.Name,
                                UnitPrice = 999999m, // deliberately wrong — service must re-price from DB
                                Quantity = buyQty
                            }
                        }
                    };

                    var created = orderService.CreateOrder(userId, "456 Happy Path Ave", goodCart);
                    if (created == null || created.OrderId <= 0)
                    {
                        Console.WriteLine("FAIL: CreateOrder did not return a persisted order");
                        return 1;
                    }

                    if (created.Items == null || created.Items.Count != 1)
                    {
                        Console.WriteLine("FAIL: expected 1 order item");
                        return 1;
                    }

                    if (created.Items[0].UnitPrice != product.Price)
                    {
                        Console.WriteLine("FAIL: UnitPrice should be snapshotted from DB ({0}), got {1}",
                            product.Price, created.Items[0].UnitPrice);
                        return 1;
                    }

                    if (created.TotalAmount != product.Price * buyQty)
                    {
                        Console.WriteLine("FAIL: TotalAmount mismatch");
                        return 1;
                    }

                    context.Entry(product).Reload();
                    if (product.Stock != originalStock - buyQty)
                    {
                        Console.WriteLine("FAIL: stock not decremented. Expected {0}, got {1}",
                            originalStock - buyQty, product.Stock);
                        return 1;
                    }

                    var dbOrder = context.Orders.Include("OrderItems").First(o => o.OrderId == created.OrderId);
                    if (dbOrder.OrderItems.Count != 1 || dbOrder.OrderItems.First().Quantity != buyQty)
                    {
                        Console.WriteLine("FAIL: OrderItem rows not persisted correctly");
                        return 1;
                    }

                    Console.WriteLine("PASS: happy path created order {0}, stock {1} → {2}",
                        created.OrderId, originalStock, product.Stock);

                    // Cleanup: restore stock, remove smoke order
                    foreach (var item in dbOrder.OrderItems.ToList())
                    {
                        context.OrderItems.Remove(item);
                    }
                    context.Orders.Remove(dbOrder);
                    product.Stock = originalStock;
                    context.SaveChanges();

                    Console.WriteLine("PASS: Spec 04 service-layer acceptance complete (cleanup done)");
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
