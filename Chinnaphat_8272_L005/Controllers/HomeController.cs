using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Chinnaphat_8272_L005.Models;
using Chinnaphat_8272_L005.ViewModels;
using System.ComponentModel.Design;

namespace Chinnaphat_8272_L005.Controllers;

public class HomeController : Controller
{
    private readonly Csi402dbContext _db;

    public HomeController (Csi402dbContext db)
    {
        _db = db;
    }

    public IActionResult Lab9()
    {
        //Create || Insert Value in SQL
        return View();
    }   
    [HttpPost]
    public IActionResult Lab9(Labstudent data)
    {
        if (data.StdPassword != null)
        {
            if (ModelState.IsValid)
            {
                var CheckStd = (from a in _db.Labstudents where a.StdId == data.StdId select a).FirstOrDefault();
                if (CheckStd == null)
                {
                    Labstudent s = new();
                    s.StdId = data.StdId;
                    s.StdPassword = data.StdPassword;
                    s.StdName = data.StdName;
                    s.StdLastname = data.StdLastname;
                    _db.Add(s);
                    _db.SaveChanges();
                    return View(data);
                }
                else
                {
                    ViewBag.Status = "StdCode ซํ้า";
                    return View(data);
                }
            }
            else
            {
                ViewBag.Status = "Error";
                return View(data);
            }
        }
        else
        {
            ViewBag.Status = "Password Null";
            return View(data);
        }
    }

    public IActionResult Lab9_1()
    {
        var Student = (from s in _db.Labstudents select s).ToList();
        return View(Student);
    }
    

    public IActionResult Index()
    {
        // สร้าง IndexViewModel จากข้อมูลที่ UserController ส่งมาผ่าน TempData
        var model = new IndexViewModel
        {
            IsLoggedIn = TempData["IsLoggedIn"] is bool b && b,
            Username   = TempData["Username"]  as string ?? string.Empty,
            FullName   = TempData["FullName"]   as string ?? string.Empty,
            Email      = TempData["Email"]      as string ?? string.Empty,
            LoginTime  = DateTime.TryParse(TempData["LoginTime"] as string, out var lt)
                             ? lt : DateTime.Now,
        };
        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Customer()
    {
        ViewData["Title"] = "หน้าหลัก";
        return View();
    }

    public IActionResult Admin()
    {
        ViewData["Title"] = "Dashboard";
        return View();
    }

  public IActionResult Lab5(string Name, string LastName)
    {
        var Data = new Lab5ViewModel();
        Data.Name = Name;
        Data.LastName = LastName;
        return View(Data);
    }
    public IActionResult Lab5_2()
    {
        var User = new List<Lab5ViewModel>
        {
            new Lab5ViewModel{Name = "a",LastName = "b"},
            new Lab5ViewModel{Name = "c",LastName = "d"}
        };
        return View(User);
    }

    public IActionResult Lab5_3()
    {
        return View();
    }
    [HttpPost]
    public IActionResult Lab5_3(Lab5ViewModel data)
    {
        string a, b, c;
        a = data.Name;
        b = data.LastName;
        // c = a + " " + b;
        c = data.Name + " " + data.LastName;
        ViewBag.Name = c;
        // return View(data);
        return RedirectToAction("Lab5", "Home", new { Name = data.Name, LastName = data.LastName });
    }

 public IActionResult Lab2()
    {
    char A = 'a';
       string Name, Lastname, Class, Years, Code;

    Name = "Chinnaphat";
    Lastname = "Supsombat";
    Class = "L005";
    Years = "3Years";
    Code = "none";

    ViewBag.Name = Name;
    ViewBag.Lastname = Lastname;
    ViewBag.Class = Class;
    ViewBag.Years = Years;
    ViewBag.Code = Code;
    
    int a,b,c,d,e,f,g,h,i,j,sum;
    a=1;
    b=2;
    c=3;
    d=4;
    e=5;
    f=6;
    g=7;
    h=8;
    i=9;
    j=10;
    
    ViewBag.a = a;
    ViewBag.b = b;
    ViewBag.c = c;
    ViewBag.d = d;
    ViewBag.e = e;
    ViewBag.f = f;
    ViewBag.g = g;
    ViewBag.h = h;
    ViewBag.i = i;
    ViewBag.j = j; 
    
    sum = a+b+c+d+e+f+g+h+i+j ;



    ViewBag.sum =sum;
    if (sum > 80)
    {
        ViewBag.Result = "a";
    }
      if (sum > 76)
    {
        ViewBag.Result = "b+";
    }
      if (sum > 70)
    {
        ViewBag.Result = "b";
    }
      if (sum > 66)
    {
        ViewBag.Result = "c+";
    }
      if (sum > 60)
    {
        ViewBag.Result = "c";
    }
      if (sum > 56)
    {
        ViewBag.Result = "d+";
    }
      if (sum > 50)
    {
        ViewBag.Result = "d";
    }
      if (sum < 49)
    {
        ViewBag.Result = "f";
    }
    return View();
    }

    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
