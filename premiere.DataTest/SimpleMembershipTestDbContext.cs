using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using MySql.Web.Security;
using MySql.Data.MySqlClient;

namespace premiere.DataTest
{
	public class SimpleMembershipDbContext : MySqlSecurityDbContext
	{
		// public non argument constructor for MySqlSimpleMembershipProvider
		public SimpleMembershipDbContext()
			: base("SimpleMembershipTestDbContext")
		{
		}

		public static SimpleMembershipDbContext CreateContext()
		{
			return new SimpleMembershipDbContext();
		}

		public DbSet<UserProperty> UserProperties
		{
			get;
			set;
		}
	}
}
