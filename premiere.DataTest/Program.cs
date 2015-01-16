using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Web.Security;

namespace premiere.DataTest
{
	class Program
	{
		private static void Main()
		{
			using (var db = SimpleMembershipDbContext.CreateContext())
			{
				
				var count = db.UserProfiles.Count();

				Console.WriteLine("user count: {0}", count);

				MySqlWebSecurity.InitializeDatabaseConnection("SimpleMembershipTestDbContext");

				int userId = MySqlWebSecurity.GetUserId("admin");

				Console.WriteLine("user ID: {0}", userId);

				db.OAuthMemberships.Add(new OAuthMembership
				{
					Provider = "ds",
					ProviderUserId = "dsd",
					UserId = userId,
				});

				int ret = db.SaveChanges();
			}
		}
	}
}
