using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ONLINE_TICKET_BOOKING_SYSTEM.Data;
using ONLINE_TICKET_BOOKING_SYSTEM.Models;
using ONLINE_TICKET_BOOKING_SYSTEM.ViewModels;
using System.Linq;
using System.Threading.Tasks;

public class UserManagementController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    public UserManagementController(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    // GET: /UserManagement/Index
    public IActionResult Index()
    {
        var users = _userManager.Users.ToList(); // Get all users from Identity

        return View(users);
    }
    public IActionResult Operator()
    {
        var operatorSummary = _context.Buses
    .AsEnumerable() // Bring data into memory
    .GroupBy(b => b.OperatorName)
    .Select(g => new OperatorSummaryViewModel
    {
        OperatorName = g.Key,
        TotalBuses = g.Count(),
        TotalRoutes = g.Select(x => new { x.From, x.To }).Distinct().Count(),
        TotalSeats = g.Sum(x => x.SeatsAvailable),
         Fares = string.Join(", ", g.Select(x => x.Fare.ToString("0.##")).Distinct())
    })
    .ToList();


        return View(operatorSummary);
    }

}
