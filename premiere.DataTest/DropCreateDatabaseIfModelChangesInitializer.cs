using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using MySql.Web.Security;

namespace premiere.DataTest
{
	public class DropCreateDatabaseIfModelChangesInitializer : DropCreateMySqlDatabaseIfModelChanges<SimpleMembershipDbContext>
	{
		protected override void Seed(SimpleMembershipDbContext db)
		{
			db.UserProperties.Add(new UserProperty
			{
				UserId = 1,
				UserName = "admin",
				Age = 40,
				Email = "xyz3710@gmail.com",
				Facebook = "http://facebook.com/xyz37",
				Rate = 100,
				LastName = "Kim",
				FirstName = "Ki Won",
			});
		}
	}
}
