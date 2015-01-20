using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using MySql.Web.Security;
using System.Web.Security;

namespace premiere.Data.Controllers
{
	public class HomeController : Controller
	{
		public ActionResult Index()
		{
			ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";

            //MySQLRoleProvider mysqlRoleProvider = new System.Web.Security.RoleProvider() as MySQLRoleProvider;

           
            System.Web.Security.RoleProvider previousRoleProvider = new MySqlSimpleRoleProvider();

            ViewBag.Role = previousRoleProvider.GetRolesForUser("robin521"); 


			return View();
		}

		public ActionResult About()
		{
			ViewBag.Message = "Your app description page.";

			return View();
		}

		public ActionResult Contact()
		{
			ViewBag.Message = "Your contact page.";

			return View();
		}
	}
}
