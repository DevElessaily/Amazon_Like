using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using portofolio3.Data;
using portofolio3.Models;

namespace portofolio3.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();

            var rm = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            foreach (var r in new[] { "User", "Seller", "Admin" })
                if (!await rm.RoleExistsAsync(r))
                    await rm.CreateAsync(new IdentityRole(r));

            var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            await EnsureIdentity(um, "teddy@portofolio.com", "Seller@123", "Seller");
            await EnsureIdentity(um, "nadia@portofolio.com", "Seller@123", "Seller");
            await EnsureIdentity(um, "admin@portofolio.com", "Admin@123", "Admin");
            await EnsureIdentity(um, "buyer@portofolio.com", "Buyer@123", "User");

            var categories = new Dictionary<string, Category>();
            foreach (var c in await db.Categories.ToListAsync())
                categories[c.Name] = c;
            foreach (var c in new[]
            {
                new Category { Name = "Electronics", Description = "Gadgets & audio" },
                new Category { Name = "Fashion", Description = "Apparel & accessories" },
                new Category { Name = "Home & Kitchen", Description = "Everyday essentials" }
            })
            {
                if (!categories.ContainsKey(c.Name))
                {
                    db.Categories.Add(c);
                    categories[c.Name] = c;
                }
            }
            await db.SaveChangesAsync();

            var sellers = new Dictionary<string, Seller>();
            foreach (var s in await db.Sellers.ToListAsync())
                sellers[s.Email] = s;
            foreach (var s in new[]
            {
                new Seller { FullName = "Teddy Osei", ShopName = "TechNest", Email = "teddy@portofolio.com", Phone = "0100 123 4567", PasswordHash = "", ProfileImage = "", IsApproved = true, CreatedAt = DateTime.UtcNow },
                new Seller { FullName = "Nadia Rania", ShopName = "Nadia Style", Email = "nadia@portofolio.com", Phone = "0111 111 2222", PasswordHash = "", ProfileImage = "", IsApproved = true, CreatedAt = DateTime.UtcNow }
            })
            {
                if (!sellers.ContainsKey(s.Email))
                {
                    db.Sellers.Add(s);
                    sellers[s.Email] = s;
                }
            }
            await db.SaveChangesAsync();

            var tech = categories["Electronics"];
            var fashion = categories["Fashion"];
            var home = categories["Home & Kitchen"];
            var techNest = sellers["teddy@portofolio.com"];
            var nadiaStyle = sellers["nadia@portofolio.com"];

            var existingNames = (await db.Products.Select(p => p.Name).ToListAsync()).ToHashSet();
            foreach (var p in new[]
            {
                new Product { Name = "Wireless Over-Ear Headphones", Description = "40h battery, ANC, bluetooth 5.3.", Price = 129.99m, Stock = 25, SellerId = techNest.Id, CategoryId = tech.Id, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new Product { Name = "Smart Watch Fitness Tracker", Description = "Heart rate, GPS, 7-day battery.", Price = 79.50m, Stock = 40, SellerId = techNest.Id, CategoryId = tech.Id, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new Product { Name = "Classic Denim Jacket", Description = "Unisex, stone-washed.", Price = 54.00m, Stock = 18, SellerId = nadiaStyle.Id, CategoryId = fashion.Id, IsApproved = true, CreatedAt = DateTime.UtcNow },
                new Product { Name = "Gooseneck Electric Kettle", Description = "Temp control, 0.9L.", Price = 38.75m, Stock = 22, SellerId = techNest.Id, CategoryId = home.Id, IsApproved = true, CreatedAt = DateTime.UtcNow }
            })
            {
                if (!existingNames.Contains(p.Name))
                    db.Products.Add(p);
            }
            await db.SaveChangesAsync();

            var products = await db.Products.ToListAsync();
            foreach (var p in products)
            {
                if (!await db.ProductImages.AnyAsync(i => i.ProductId == p.Id))
                {
                    var img = p.Name switch
                    {
                        "Wireless Over-Ear Headphones" => "/images/headphones.svg",
                        "Smart Watch Fitness Tracker" => "/images/smartwatch.svg",
                        "Classic Denim Jacket" => "/images/denim.svg",
                        "Gooseneck Electric Kettle" => "/images/kettle.svg",
                        _ => null
                    };
                    if (img != null)
                        db.ProductImages.Add(new ProductImage { ProductId = p.Id, ImageUrl = img });
                }
            }
            await db.SaveChangesAsync();

            if (!await db.Reviews.AnyAsync())
            {
                var buyer = await db.Users.FirstOrDefaultAsync(u => u.Email == "buyer@portofolio.com");
                if (buyer == null)
                {
                    buyer = new User { Name = "Buyer", Email = "buyer@portofolio.com", PasswordHash = "x", CreatedAt = DateTime.UtcNow };
                    db.Users.Add(buyer);
                    await db.SaveChangesAsync();
                }

                var headphones = products.FirstOrDefault(p => p.Name == "Wireless Over-Ear Headphones");
                var denim = products.FirstOrDefault(p => p.Name == "Classic Denim Jacket");
                if (headphones != null)
                    db.Reviews.Add(new Review { ProductId = headphones.Id, UserId = buyer.Id, Rating = 5, Comment = "Amazing sound.", CreatedAt = DateTime.UtcNow });
                if (denim != null)
                    db.Reviews.Add(new Review { ProductId = denim.Id, UserId = buyer.Id, Rating = 4, Comment = "Great fit.", CreatedAt = DateTime.UtcNow });
                await db.SaveChangesAsync();
            }
        }

        private static async Task<ApplicationUser> EnsureIdentity(UserManager<ApplicationUser> um, string email, string password, string? role)
        {
            var user = await um.FindByEmailAsync(email);
            if (user != null) return user;
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true };
            var r = await um.CreateAsync(user, password);
            if (r.Succeeded && !string.IsNullOrEmpty(role))
                await um.AddToRoleAsync(user, role);
            return user;
        }
    }
}