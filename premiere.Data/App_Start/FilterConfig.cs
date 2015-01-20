using System.Web;
using System.Web.Mvc;
using premiere.Data.Filters;

namespace premiere.Data
{
	public class FilterConfig
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
            filters.Add(new InitializeSimpleMembershipAttribute());
		}
	}
}