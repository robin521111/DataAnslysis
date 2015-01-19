using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using DotNetOpenAuth.AspNet;
using Microsoft.Web.WebPages.OAuth;
using MySql.Data.MySqlClient;
using MySql.Web.Security;
using premiere.Data;
using premiere.Data.Filters;
using premiere.DataTest;
using premiere.Data.Models;
using WebMatrix.WebData;

namespace premiere.Data.Controllers
{
	[Authorize]
	[InitializeSimpleMembership]
	public class AccountController : Controller
	{
		//
		// GET: /Account/Login

		[AllowAnonymous]
		public ActionResult Login(string returnUrl)
		{
			ViewBag.ReturnUrl = returnUrl;
			return View();
		}

		//
		// POST: /Account/Login

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult Login(LoginModel model, string returnUrl)
		{
			if (ModelState.IsValid && MySqlWebSecurity.Login(model.UserName, model.Password, persistCookie: model.RememberMe))
			{
				return RedirectToLocal(returnUrl);
			}

			// If we got this far, something failed, redisplay form
			ModelState.AddModelError("", "The user name or password provided is incorrect.");
			return View(model);
		}

		//
		// POST: /Account/LogOff

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult LogOff()
		{
			MySqlWebSecurity.Logout();

			return RedirectToAction("Index", "Home");
		}

        public ActionResult Logoff()
        {
            MySqlWebSecurity.Logout();

            return RedirectToAction("Index","Home");
        }

		//
		// GET: /Account/Register

		[AllowAnonymous]
		public ActionResult Register()
		{
			return View();
		}

		//
		// POST: /Account/Register

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult Register(RegisterModel model)
		{
			if (ModelState.IsValid)
			{
				// Attempt to register the user
				try
				{
					IDictionary<string, object> properties = new Dictionary<string, object>();

					// NOTICE: To use this property columns. Add "MySql.Data.Extension" project partial "UserProfile" class and add property columns.
					// by KIM-KIWON\xyz37(Kim Ki Won) in Thursday, April 18, 2013 5:02 PM
					//properties.Add("Email", model.Email);
					//properties.Add("Facebook", model.Facebook);
					//properties.Add("Age", model.Age);
					//properties.Add("Rate", model.Rate);

					using (TransactionScope scope = new TransactionScope())
					{
						MySqlWebSecurity.CreateUserAndAccount(model.UserName, model.Password, properties);
						MySqlWebSecurity.Login(model.UserName, model.Password);

						var userId = MySqlWebSecurity.GetUserId(model.UserName);

						using (var db = SimpleMembershipDbContext.CreateContext())
						{
							db.UserProperties.Add(new UserProperty
							{
								UserId = userId,
								UserName = model.UserName,
								Age = model.Age,
								Email = model.Email,
								Facebook = model.Facebook,
								Rate = model.Rate,
								LastName = model.LastName,
								FirstName = model.FirstName,
							});

                            db.UsersInRoles.Add(new UsersInRoles{
                                UserId = userId,
                                RoleId = 2

                            });
							db.SaveChanges();
                           
						}

						scope.Complete();
					}


					return RedirectToAction("Index", "Home");
				}
				catch (MembershipCreateUserException e)
				{
					ModelState.AddModelError("", ErrorCodeToString(e.StatusCode));
				}
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		//
		// POST: /Account/Disassociate

		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult Disassociate(string provider, string providerUserId)
		{
			string ownerAccount = OAuthWebSecurity.GetUserName(provider, providerUserId);
			ManageMessageId? message = null;

			// Only disassociate the account if the currently logged in user is the owner
			if (ownerAccount == User.Identity.Name)
			{
				// Use a transaction to prevent the user from deleting their last login credential
				using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions
				{
					IsolationLevel = IsolationLevel.Serializable
				}))
				{
					bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(MySqlWebSecurity.GetUserId(User.Identity.Name));
					int externalLoginCount = OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name).Count;

					if (hasLocalAccount == true || externalLoginCount > 1)
					{
						OAuthWebSecurity.DeleteAccount(provider, providerUserId);
						scope.Complete();
						message = ManageMessageId.RemoveLoginSuccess;
					}
					else if (hasLocalAccount == false && externalLoginCount == 1)
						message = ManageMessageId.RequestOneExternalLogin;
				}
			}

			return RedirectToAction("Manage", new
			{
				Message = message
			});
		}

		//
		// GET: /Account/Manage

		public ActionResult Manage(ManageMessageId? message)
		{
			ViewBag.StatusMessage =
				message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
				: message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
				: message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
				: message == ManageMessageId.RequestOneExternalLogin ? "You must one external login or local account."
				: "";
			ViewBag.HasLocalPassword = OAuthWebSecurity.HasLocalAccount(MySqlWebSecurity.GetUserId(User.Identity.Name));
			ViewBag.ReturnUrl = Url.Action("Manage");
			var model = new ChangePropertyModel
			{
				LocalPasswordModel = new LocalPasswordModel(),
				PropertyModel = new PropertyModel(),
			};

			using (var db = SimpleMembershipDbContext.CreateContext())
			{
				var userProperties = db.UserProperties.SingleOrDefault(x => x.UserName == User.Identity.Name);

				if (userProperties != null)
				{
					model.PropertyModel = new PropertyModel
					{
						Age = userProperties.Age,
						Email = userProperties.Email,
						Facebook = userProperties.Facebook,
						FirstName = userProperties.FirstName,
						LastName = userProperties.LastName,
						Rate = userProperties.Rate,
					};
				}
			}

			return View(model);
		}

		//
		// POST: /Account/Manage

		[HttpPost]
		//[ValidateAntiForgeryToken]
		public ActionResult Manage(ChangePropertyModel model)
		{
			bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(MySqlWebSecurity.GetUserId(User.Identity.Name));
			ViewBag.HasLocalPassword = hasLocalAccount;
			ViewBag.ReturnUrl = Url.Action("Manage");
			if (hasLocalAccount)
			{
				if (ModelState.IsValid)
				{
					// ChangePassword will throw an exception rather than return false in certain failure scenarios.
					bool changePasswordSucceeded = false;

					if (string.IsNullOrEmpty(model.LocalPasswordModel.ConfirmPassword) == true)
					{
						using (var db = SimpleMembershipDbContext.CreateContext())
						{
							var userProperty = db.UserProperties.SingleOrDefault(x => x.UserName == User.Identity.Name);

							if (userProperty == null)
							{
								var userId = MySqlWebSecurity.GetUserId(User.Identity.Name);

								userProperty = new UserProperty
								{
									UserId = userId,
									UserName = User.Identity.Name,
								};
								db.UserProperties.Add(userProperty);
							}

							userProperty.Age = model.PropertyModel.Age;
							userProperty.Email = model.PropertyModel.Email;
							userProperty.Facebook = model.PropertyModel.Facebook;
							userProperty.FirstName = model.PropertyModel.FirstName;
							userProperty.LastName = model.PropertyModel.LastName;
							userProperty.Rate = model.PropertyModel.Rate;

							changePasswordSucceeded = db.SaveChanges() > 0;
						}
					}
					else
					{
						try
						{
							changePasswordSucceeded = MySqlWebSecurity.ChangePassword(User.Identity.Name, model.LocalPasswordModel.OldPassword, model.LocalPasswordModel.NewPassword);
						}
						catch (Exception)
						{
							changePasswordSucceeded = false;
						}
					}

					if (changePasswordSucceeded == true)
					{
						return RedirectToAction("Manage", new
						{
							Message = ManageMessageId.ChangePasswordSuccess
						});
					}
					else
					{
						ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
					}
				}
			}
			else
			{
				// User does not have a local password so remove any validation errors caused by a missing
				// OldPassword field
				ModelState state = ModelState["OldPassword"];
				if (state != null)
				{
					state.Errors.Clear();
				}

				if (ModelState.IsValid)
				{
					try
					{
						MySqlWebSecurity.CreateAccount(User.Identity.Name, model.LocalPasswordModel.NewPassword);
						return RedirectToAction("Manage", new
						{
							Message = ManageMessageId.SetPasswordSuccess
						});
					}
					catch (Exception e)
					{
						ModelState.AddModelError("", e);
					}
				}
			}

			// If we got this far, something failed, redisplay form
			return View(model);
		}

		//
		// POST: /Account/ExternalLogin

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult ExternalLogin(string provider, string returnUrl)
		{
			return new ExternalLoginResult(provider, Url.Action("ExternalLoginCallback", new
			{
				ReturnUrl = returnUrl
			}));
		}

		//
		// GET: /Account/ExternalLoginCallback

		[AllowAnonymous]
		public ActionResult ExternalLoginCallback(string returnUrl)
		{
			AuthenticationResult result = OAuthWebSecurity.VerifyAuthentication(Url.Action("ExternalLoginCallback", new
			{
				ReturnUrl = returnUrl
			}));
			if (!result.IsSuccessful)
			{
				return RedirectToAction("ExternalLoginFailure");
			}

			if (OAuthWebSecurity.Login(result.Provider, result.ProviderUserId, createPersistentCookie: false))
			{
				return RedirectToLocal(returnUrl);
			}

			if (User.Identity.IsAuthenticated)
			{
				// If the current user is logged in add the new account
				OAuthWebSecurity.CreateOrUpdateAccount(result.Provider, result.ProviderUserId, User.Identity.Name);
				return RedirectToLocal(returnUrl);
			}
			else
			{
				// User is new, ask for their desired membership name
				string loginData = OAuthWebSecurity.SerializeProviderUserId(result.Provider, result.ProviderUserId);
				ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(result.Provider).DisplayName;
				ViewBag.ReturnUrl = returnUrl;
				return View("ExternalLoginConfirmation", new RegisterExternalLoginModel
				{
					UserName = result.UserName,
					ExternalLoginData = loginData
				});
			}
		}

		//
		// POST: /Account/ExternalLoginConfirmation

		[HttpPost]
		[AllowAnonymous]
		[ValidateAntiForgeryToken]
		public ActionResult ExternalLoginConfirmation(RegisterExternalLoginModel model, string returnUrl)
		{
			string provider = null;
			string providerUserId = null;

			if (User.Identity.IsAuthenticated || !OAuthWebSecurity.TryDeserializeProviderUserId(model.ExternalLoginData, out provider, out providerUserId))
			{
				return RedirectToAction("Manage");
			}

			if (ModelState.IsValid)
			{
				// Insert a new user into the database
				using (var db = SimpleMembershipDbContext.CreateContext())
				{
					UserProfile user = db.UserProfiles.FirstOrDefault(u => u.UserName.ToLower() == model.UserName.ToLower());
					// Check if user already exists
					if (user == null)
					{
						// Insert name into the profile table
						db.UserProfiles.Add(new UserProfile
						{
							UserName = model.UserName
						});
						db.SaveChanges();

						OAuthWebSecurity.CreateOrUpdateAccount(provider, providerUserId, model.UserName);
						OAuthWebSecurity.Login(provider, providerUserId, createPersistentCookie: false);

						return RedirectToLocal(returnUrl);
					}
					else
					{
						ModelState.AddModelError("UserName", "User name already exists. Please enter a different user name.");
					}
				}
			}

			ViewBag.ProviderDisplayName = OAuthWebSecurity.GetOAuthClientData(provider).DisplayName;
			ViewBag.ReturnUrl = returnUrl;
			return View(model);
		}

		//
		// GET: /Account/ExternalLoginFailure

		[AllowAnonymous]
		public ActionResult ExternalLoginFailure()
		{
			return View();
		}

		[AllowAnonymous]
		[ChildActionOnly]
		public ActionResult ExternalLoginsList(string returnUrl)
		{
			ViewBag.ReturnUrl = returnUrl;

			// Return OAuth Providers does not used.
			ICollection<AuthenticationClientData> model = null;

			if (User.Identity.Name == string.Empty)
				model = OAuthWebSecurity.RegisteredClientData;
			else
			{
				var userOAuthProviders = OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name).Select(x => x.Provider);
				model = OAuthWebSecurity.RegisteredClientData.Where(x => userOAuthProviders.Contains(x.AuthenticationClient.ProviderName) == false).ToList();
			}

			return PartialView("_ExternalLoginsListPartial", model);
		}

		[ChildActionOnly]
		public ActionResult RemoveExternalLogins()
		{
			ICollection<OAuthAccount> accounts = OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name);
			List<ExternalLogin> externalLogins = new List<ExternalLogin>();
			foreach (OAuthAccount account in accounts)
			{
				AuthenticationClientData clientData = OAuthWebSecurity.GetOAuthClientData(account.Provider);

				externalLogins.Add(new ExternalLogin
				{
					Provider = account.Provider,
					ProviderDisplayName = clientData.DisplayName,
					ProviderUserId = account.ProviderUserId,
				});
			}

			ViewBag.ShowRemoveButton = externalLogins.Count > 1 || OAuthWebSecurity.HasLocalAccount(MySqlWebSecurity.GetUserId(User.Identity.Name));
			return PartialView("_RemoveExternalLoginsPartial", externalLogins);
		}

		#region Helpers
		private ActionResult RedirectToLocal(string returnUrl)
		{
			if (Url.IsLocalUrl(returnUrl))
			{
				return Redirect(returnUrl);
			}
			else
			{
				return RedirectToAction("Index", "Home");
			}
		}

		public enum ManageMessageId
		{
			ChangePasswordSuccess,
			SetPasswordSuccess,
			RemoveLoginSuccess,
			RequestOneExternalLogin,
		}

		internal class ExternalLoginResult : ActionResult
		{
			public ExternalLoginResult(string provider, string returnUrl)
			{
				Provider = provider;
				ReturnUrl = returnUrl;
			}

			public string Provider
			{
				get;
				private set;
			}
			public string ReturnUrl
			{
				get;
				private set;
			}

			public override void ExecuteResult(ControllerContext context)
			{
				OAuthWebSecurity.RequestAuthentication(Provider, ReturnUrl);
			}
		}

		private static string ErrorCodeToString(MembershipCreateStatus createStatus)
		{
			// See http://go.microsoft.com/fwlink/?LinkID=177550 for
			// a full list of status codes.
			switch (createStatus)
			{
				case MembershipCreateStatus.DuplicateUserName:
					return "User name already exists. Please enter a different user name.";

				case MembershipCreateStatus.DuplicateEmail:
					return "A user name for that e-mail address already exists. Please enter a different e-mail address.";

				case MembershipCreateStatus.InvalidPassword:
					return "The password provided is invalid. Please enter a valid password value.";

				case MembershipCreateStatus.InvalidEmail:
					return "The e-mail address provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.InvalidAnswer:
					return "The password retrieval answer provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.InvalidQuestion:
					return "The password retrieval question provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.InvalidUserName:
					return "The user name provided is invalid. Please check the value and try again.";

				case MembershipCreateStatus.ProviderError:
					return "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

				case MembershipCreateStatus.UserRejected:
					return "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

				default:
					return "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
			}
		}
		#endregion
	}
}
