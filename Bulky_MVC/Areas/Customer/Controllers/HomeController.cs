using System.Diagnostics;
using System.Security.Claims;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.DataAccess.Data;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bulky_MVC.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unit;
        private readonly AppDbContext _context;
        public HomeController(ILogger<HomeController> logger , IUnitOfWork unit, AppDbContext context)
        {
            _logger = logger;
            _unit = unit;
            _context = context;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> productList = _unit.Product.GetAll(includeProperities:"Category");
            return View(productList);
        }
        public IActionResult Details(int id)
        {
            ShoppingCart shoppingCart = new()
            {
                Product = _unit.Product.Get(u => u.Id == id, includeProperities: "Category"),
                Count = 1,
                ProductId = id
            };
            
            return View(shoppingCart);
           
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            shoppingCart.AppUserId = userId;

            var cartFromDb = await _context.ShoppingCarts
                .FirstOrDefaultAsync(s => s.AppUserId == userId);

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == shoppingCart.ProductId);

            if (product is null)
            {
                return View(); 
            }

            if (cartFromDb != null)
            {
                // Update existing cart entry
                cartFromDb.Count += shoppingCart.Count;
                _context.ShoppingCarts.Update(cartFromDb); 
            }
            else
            {
                // Add new cart record (ID should be auto-generated)
                var newCart = new ShoppingCart
                {
                    ProductId = shoppingCart.ProductId,
                    AppUserId = shoppingCart.AppUserId,
                    Product = product,
                    Count = shoppingCart.Count
                };

                await _context.ShoppingCarts.AddAsync(newCart);
            }

            await _context.SaveChangesAsync(); // Ensure async save

            TempData["success"] = "Cart updated successfully";
            return View();
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
