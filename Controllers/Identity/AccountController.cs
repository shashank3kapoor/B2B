using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using StandAloneB2B.Presentation.Framework;
using StandAloneB2B.Presentation.Models;
using StandAloneB2B.Presentation.Providers;
using StandAloneB2B.Presentation.Results;
using StandAloneB2B.Common.Email;
using StandAloneB2B.Presentation.Framework.Identity;
using StandAloneB2B.ServiceFramework.UserAdministration;
using StandAloneB2B.ServiceFramework.Identity;
using Newtonsoft.Json.Linq;


namespace StandAloneB2B.Presentation.Controllers.Identity
{
    [Authorize]
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        private const string LocalLoginProvider = "Local";
        private UserManager _UserManager;
        private IUserCodeService userCodeService;
        private IUserService userService;
        private IAdministrationSecurityService administrationSecurityService;

        public AccountController(IUserCodeService userCodeService, IUserService userService, IAdministrationSecurityService administrationSecurityService)
        {
            this.userCodeService = userCodeService;
            this.userService = userService;
            this.administrationSecurityService = administrationSecurityService;
        }

        public AccountController(UserManager UserManager,
            ISecureDataFormat<AuthenticationTicket> accessTokenFormat)
        {
            _UserManager = UserManager;
            AccessTokenFormat = accessTokenFormat;
        }

        public UserManager UserManager
        {
            get
            {
                return _UserManager ?? Request.GetOwinContext().GetUserManager<UserManager>();
            }
            private set
            {
                _UserManager = value;
            }
        }

        public ISecureDataFormat<AuthenticationTicket> AccessTokenFormat { get; private set; }

        // GET api/Account/UserInfo
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("UserInfo")]
        public UserInfoViewModel GetUserInfo()
        {
            ClaimsIdentity identity = User.Identity as ClaimsIdentity;
            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(identity);
            var dbUser  = UserManager.FindById(User.Identity.GetUserId<int>());
            var userInfo = new UserInfoViewModel
            {
                Id = dbUser.Id,
                Email = User.Identity.GetUserName(),
                HasRegistered = externalLogin == null,
                LoginProvider = externalLogin?.LoginProvider,
                FirstName = identity.FindFirst(ClaimTypes.GivenName)?.Value,
                LastName = identity.FindFirst(ClaimTypes.Surname)?.Value,
                Locale = identity.FindFirst(ClaimTypes.Locality)?.Value,
                Claims = identity.Claims,
                PasswordExpiration = dbUser.PasswordExpirationDateUtc != null ? ((dbUser.PasswordExpirationDateUtc.Value - DateTime.UtcNow ).Days >= 0 ? ((dbUser.PasswordExpirationDateUtc.Value - DateTime.UtcNow).Days == 0 && (dbUser.PasswordExpirationDateUtc.Value - DateTime.UtcNow).Hours > 0 ? "Password will expire today at" + dbUser.PasswordExpirationDateUtc.Value.ToLocalTime().ToString("hh:mm T") :  "Password will expire in " + (dbUser.PasswordExpirationDateUtc.Value - DateTime.UtcNow).Days.ToString() + " days") : "Your password has expired") : "",
                LastLoginDate = (dbUser.LastLoginDateUtc != null && dbUser.TimezoneID != null) ? TimeZoneInfo.ConvertTimeFromUtc(dbUser.LastLoginDateUtc.Value, TimeZoneInfo.FindSystemTimeZoneById(dbUser.TimezoneID)) : (dbUser.LastLoginDateUtc != null ? dbUser.LastLoginDateUtc.Value.ToLocalTime() : (DateTime?)null)
            };

            return userInfo;
        }

        // POST api/Account/Logout
        [Route("Logout")]
        public IHttpActionResult Logout()
        {
            Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
            return Ok();
        }

        // GET api/Account/ManageInfo?returnUrl=%2F&generateState=true
        [Route("ManageInfo")]
        public async Task<ManageInfoViewModel> GetManageInfo(string returnUrl, bool generateState = false)
        {
            DataAccess.Models.User User = await UserManager.FindByIdAsync(HttpContext.Current.User.Identity.GetUserId<int>());
            if (User == null)
            {
                return null;

               
            }

            List<UserLoginInfoViewModel> logins = new List<UserLoginInfoViewModel>();

            foreach (var linkedAccount in User.UserLogins)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = linkedAccount.LoginProvider,
                    ProviderKey = linkedAccount.ProviderKey
                });
            }

            if (User.PasswordHash != null)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = LocalLoginProvider,
                    ProviderKey = User.UserName,
                });
            }

            return new ManageInfoViewModel
            {
                LocalLoginProvider = LocalLoginProvider,
                Email = User.UserName,
                Logins = logins,
                ExternalLoginProviders = GetExternalLogins(returnUrl, generateState)
            };
        }

        // POST api/Account/ChangePassword
        [AllowAnonymous]
        [Route("ChangePassword")]
        public async Task<IHttpActionResult> ChangePassword(ChangePasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            int userId = 0;
            DataAccess.Models.User user = null;


            if (!string.IsNullOrEmpty(model.Username))
            {
                user = await UserManager.FindByNameAsync(model.Username);
                userId = user.Id;
            }
            else
                userId = User.Identity.GetUserId<int>();

            IdentityResult result = await UserManager.ChangePasswordAsync(userId, model.OldPassword,
                model.NewPassword);

            
            if (!result.Succeeded)
            {
                var error = IdentityHelper.GetIdentityErrors(result);
                if (error.Contains("Incorrect password"))
                {
                    if (user == null)
                        user = await UserManager.FindByIdAsync(userId);

                    user.ChangePasswordFailedCount++;
                    if (user.ChangePasswordFailedCount >= 5)
                    {
                        user.ChangePasswordFailedCount = 0;
                        await UserManager.UpdateAsync(user);
                        return BadRequest("Force Logout");
                    }
                        
                    await UserManager.UpdateAsync(user);

                }

                return BadRequest(IdentityHelper.GetIdentityErrors(result));
            }
            else
            {
                DataAccess.Models.User UserDb = await UserManager.FindByIdAsync(userId);

                if(UserDb != null)
                {
                    UserDb.PasswordExpirationDateUtc = DateTime.UtcNow.AddDays(30);
                    var updateResult = await UserManager.UpdateAsync(UserDb);
                    if(!updateResult.Succeeded)
                    {
                        return BadRequest(IdentityHelper.GetIdentityErrors(result));
                    }
                }
            }

            return Ok();
        }
        
        // POST api/Account/ForgotPassword
        [AllowAnonymous]
        [HttpPost]
        [Route("ForgotPassword")]
        public async Task<IHttpActionResult> ForgotPassword(string email)
        {
            var baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await UserManager.FindByEmailAsync(email);
            if (user == null)
            {
                return BadRequest("Email does not exist");
            }
            else
            {
                try
                {
                    var userCode = userCodeService.FindByUser(user.Id);
                    if (userCode != null)
                    {
                        userCodeService.Delete(userCode.UserCodeK);
                    }

                    userCode = userCodeService.Create(new DataAccess.Models.UserCode() { UserK = user.Id });
                    userCodeService.SaveChanges();
                    string code = userCode.Code;

                    var adminSecurity = administrationSecurityService.GetAdministrationSecurity();
                    EmailDto emaildto = new EmailDto()
                    {
                        EmailBody = String.Format("This message was sent to you because someone requested a password reset on your account. <br/><br/>  Your Onetime code is: <b>{0}</b> <br/> This Onetime code is valid until: <b>{1}</b> at which time it will expire and a new one code will be required to be requested. <br/><br/> To enter your onetime code. Click on \"Forget my password\" then click on \"I have a onetime code\" <br/><br/>If you did not request this password reset, please ignore this message. <br/> Do not reply to this email message as the mail box is un-monitored.", code, userCode.ExpirationDateUtc.ToLocalTime().ToString("dd-MMMM-yyyy hh:mm tt")),
                        EmailSubject = "SITETRAX Evolution password reset",
                        EmailSender = "noreplay.StandAloneB2B.evo@gmail.com",
                        EmailRecipient = user.Email
                    };

                    CustomEmail.SendPasswordEmail(adminSecurity.MailerServer, adminSecurity.MailerServerPort.Value, adminSecurity.MailerUsername, adminSecurity.MailerPassword, adminSecurity.PasswordResetEmail, user.Email, emaildto.EmailSubject, emaildto.EmailBody);
                }
                catch (Exception ex)
                {
                    var message = ex.Message;
                }
                
                //await UserManager.SendEmailAsync(user.Id, "Forgot Password", "Please reset your password by clicking <a href=\"" + callbackUrl + "\">here</a>");
            }

            return Ok();
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("validateemailcode")]
        public IHttpActionResult ValidateEmailCode(ResetPasswordViewModel model)
        {
            var user = userService.ReadByEmail(model.Email);
            if (user == null)
            {
                return BadRequest("Email does not exist");
            }
            var userCode = userCodeService.FindByUserCode(user.Id, model.Code);
            if(userCode == null)
            {
                return BadRequest("Invalid code");
            }

            return Ok();
        }

        [AllowAnonymous]
        [Route("ResetPassword")]
        public async Task<IHttpActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var user = await UserManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return BadRequest("Invalid Email");
            }

            var userCode = userCodeService.FindByUserCode(user.Id, model.Code);
            if (userCode != null)
            {
                if (userCode.ExpirationDateUtc < DateTime.UtcNow)
                    return BadRequest("This code is already expired. Please request a new one.");

                userCodeService.Delete(userCode.UserCodeK);
                userCodeService.SaveChanges();
                user.PasswordHash = UserManager.PasswordHasher.HashPassword(model.Password);
                user.PasswordExpirationDateUtc = DateTime.UtcNow.AddMonths(administrationSecurityService.GetAdministrationSecurity().PasswordExpiryMonths);
                var updateResult = await UserManager.UpdateAsync(user);

                return Ok();
            }
            else
            {
                return BadRequest("Invalid code");
            }
            
        }


        // POST api/Account/SetPassword
        [Route("SetPassword")]
        public async Task<IHttpActionResult> SetPassword(SetPasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId<int>(), model.NewPassword);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/AddExternalLogin
        [Route("AddExternalLogin")]
        public async Task<IHttpActionResult> AddExternalLogin(AddExternalLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

            AuthenticationTicket ticket = AccessTokenFormat.Unprotect(model.ExternalAccessToken);

            if (ticket == null || ticket.Identity == null || (ticket.Properties != null
                && ticket.Properties.ExpiresUtc.HasValue
                && ticket.Properties.ExpiresUtc.Value < DateTimeOffset.UtcNow))
            {
                return BadRequest("External login failure.");
            }

            ExternalLoginData externalData = ExternalLoginData.FromIdentity(ticket.Identity);

            if (externalData == null)
            {
                return BadRequest("The external login is already associated with an account.");
            }

            IdentityResult result = await UserManager.AddLoginAsync(User.Identity.GetUserId<int>(),
                new UserLoginInfo(externalData.LoginProvider, externalData.ProviderKey));

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/RemoveLogin
        [Route("RemoveLogin")]
        public async Task<IHttpActionResult> RemoveLogin(RemoveLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result;

            if (model.LoginProvider == LocalLoginProvider)
            {
                result = await UserManager.RemovePasswordAsync(User.Identity.GetUserId<int>());
            }
            else
            {
                result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId<int>(),
                    new UserLoginInfo(model.LoginProvider, model.ProviderKey));
            }

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }
        

        // GET api/Account/ExternalLogin
        //[OverrideAuthentication]
        //[HostAuthentication(DefaultAuthenticationTypes.ExternalCookie)]

        [AllowAnonymous]
        [HttpGet]
        [Route("ExternalLogin")]
        public async Task<IHttpActionResult> GetExternalLogin(string provider, string error = null)
        {
            if (error != null)
            {
                return Redirect(Url.Content("~/") + "#error=" + Uri.EscapeDataString(error));
            }

            if (!HttpContext.Current.User.Identity.IsAuthenticated)
            {
                return new ChallengeResult(provider, this);
            }

            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(HttpContext.Current.User.Identity as ClaimsIdentity);

            if (externalLogin == null)
            {
                return InternalServerError();
            }

            if (externalLogin.LoginProvider != provider)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                return new ChallengeResult(provider, this);
            }

            DataAccess.Models.User User = await UserManager.FindAsync(new UserLoginInfo(externalLogin.LoginProvider,
                externalLogin.ProviderKey));

            bool hasRegistered = User != null;

            if (hasRegistered)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                
                 ClaimsIdentity oAuthIdentity = await UserManager.CreateIdentityAsync(User,
                    OAuthDefaults.AuthenticationType);
                ClaimsIdentity cookieIdentity = await UserManager.CreateIdentityAsync(User,
                    CookieAuthenticationDefaults.AuthenticationType);

                AuthenticationProperties properties = ApplicationOAuthProvider.CreateProperties(User.UserName);
                Authentication.SignIn(properties, oAuthIdentity, cookieIdentity);
            }
            else
            {
                IEnumerable<Claim> claims = externalLogin.GetClaims();
                ClaimsIdentity identity = new ClaimsIdentity(claims, OAuthDefaults.AuthenticationType);
                Authentication.SignIn(identity);
            }

            return Ok();
        }

        [HttpGet, Route("Roles")]
        public IHttpActionResult GetRoles()
        {
            return Ok(UserManager.GetRoles(User.Identity.GetUserId<int>()));
        }

        // GET api/Account/ExternalLogins?returnUrl=%2F&generateState=true
        [AllowAnonymous]
        [Route("ExternalLogins")]
        public IEnumerable<ExternalLoginViewModel> GetExternalLogins(string returnUrl, bool generateState = false)
        {
            IEnumerable<AuthenticationDescription> descriptions = Authentication.GetExternalAuthenticationTypes();
            List<ExternalLoginViewModel> logins = new List<ExternalLoginViewModel>();

            string state;

            if (generateState)
            {
                const int strengthInBits = 256;
                state = RandomOAuthStateGenerator.Generate(strengthInBits);
            }
            else
            {
                state = null;
            }

            foreach (AuthenticationDescription description in descriptions)
            {
                ExternalLoginViewModel login = new ExternalLoginViewModel
                {
                    Name = description.Caption,
                    Url = Url.Route("ExternalLogin", new
                    {
                        provider = description.AuthenticationType,
                        response_type = "token",
                        client_id = Startup.PublicClientId,
                        redirect_uri = new Uri(Request.RequestUri, returnUrl).AbsoluteUri,
                        state = state
                    }),
                    State = state
                };
                logins.Add(login);
            }

            return logins;
        }

        // POST api/Account/Register
        [AllowAnonymous]
        [Route("Register")]
        public async Task<IHttpActionResult> Register(RegisterBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var User = new DataAccess.Models.User() { UserName = model.Email, Email = model.Email };

            IdentityResult result = await UserManager.CreateAsync(User, model.Password);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok(new { Success = true, Message = "The user was created successfully." });
        }

        // POST api/Account/RegisterExternal
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("RegisterExternal")]
        public async Task<IHttpActionResult> RegisterExternal(RegisterExternalBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var info = await Authentication.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return InternalServerError();
            }

            var User = new DataAccess.Models.User() { UserName = model.Email, Email = model.Email };

            IdentityResult result = await UserManager.CreateAsync(User);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            result = await UserManager.AddLoginAsync(User.Id, info.Login);
            if (!result.Succeeded)
            {
                return GetErrorResult(result); 
            }
            return Ok();
        }

        [HttpPost]
        [Route("sendaccessemailreport")]
        public IHttpActionResult SendAccountEmailReport(DataAccess.Models.User user)
        {
            user = UserManager.FindById(user.Id);

            EmailDto emaildto = new EmailDto()
            {
                EmailBody = "Hi Admin! <br/><br/> The following user reported an unauthorize access on his/her account. <br><br> Id: " + user.Id + "<br/> Name: " + user.FirstName  + " " + user.LastName + "<br/> Email: " + user.Email + "<br/><br/>" + "siteTRAX has requested the user to change his/her password immediately.",
                EmailSubject = "Unauthorize Access Report",
            };

            try
            {
                var adminSecurity = administrationSecurityService.GetAdministrationSecurity();
                CustomEmail.SendLoginIssuesEmail(adminSecurity.MailerServer, adminSecurity.MailerServerPort.Value, adminSecurity.MailerUsername, adminSecurity.MailerPassword, adminSecurity.LoginIssuesAlertEmail, emaildto.EmailSubject, emaildto.EmailBody);
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong when sending the report email");
            }

            return Ok();
        }

        [HttpPut]
        [Route("resendactivationemail/{userK}")]
        public IHttpActionResult ResendActivationEmail(int userK)
        {
            var user = UserManager.FindById(userK);

            var userCode = userCodeService.FindByUser(user.Id);
            if (userCode != null)
            {
                userCodeService.Delete(userCode.UserCodeK);
            }

            userCode = userCodeService.Create(new DataAccess.Models.UserCode() { UserK = user.Id });
            userCodeService.SaveChanges();

            string code = userCode.Code;

            var adminSecurity = administrationSecurityService.GetAdministrationSecurity();
            EmailDto emaildto = new EmailDto()
            {
                EmailBody = String.Format("Hi {0} {1}. You have been added as a new user to siteTRAX Evolution. <br/><br/> Your Onetime code is: <b>{2}</b> <br/> This Onetime code is valid until: <b>{3}</b> at which time it will expire and a new one code will be required to be requested. <br/><br/> To enter your onetime code. Click on \"Forget my password\" then click on \"I have a onetime code\" <br/><br/>If you did not request this password reset, please ignore this message. <br/> Do not reply to this email message as the mail box is un-monitored.", user.FirstName, user.LastName, userCode.Code, userCode.ExpirationDateUtc.ToLocalTime().ToString("dd-MMMM-yyyy hh:mm tt")),
                EmailSubject = "New User - siteTRAX Evolution",
                EmailSender = "noreplay.StandAloneB2B.evo@gmail.com",
                EmailRecipient = user.Email
            };

            try
            {
                CustomEmail.SendPasswordEmail(adminSecurity.MailerServer, adminSecurity.MailerServerPort.Value, adminSecurity.MailerUsername, adminSecurity.MailerPassword, adminSecurity.PasswordResetEmail, user.Email, emaildto.EmailSubject, emaildto.EmailBody);
            }
            catch (Exception)
            {

                BadRequest("Failed to send an email. Please check your email address if it is valid");
            }
            

            return Ok();
        }




        [HttpPost]
        [Route("AuthorizePermissions")]
        public List<object> AuthorizePermissions()
        {
            List<object> ret=new List<object>();
            //get all user active permissions
            List<DataAccess.Models.Permission>  permissions = PermissionHelper.GetEffectivePermissions(int.Parse(User.Identity.GetUserId()));

            foreach (var permission in permissions)
            {
                ret.Add(
                    new 
                    {
                        Name = permission.Name,
                        GroupingName= permission.GroupingName,
                        Authorize= PermissionHelper.HasEffectivePermission(int.Parse(User.Identity.GetUserId()), permission.Name, permission.GroupingName)
                    }    
                );
                
            }
            return ret;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing && _UserManager != null)
            {
                _UserManager.Dispose();
                _UserManager = null;
            }

            base.Dispose(disposing);
        }

        #region Helpers

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return InternalServerError();
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        private class ExternalLoginData
        {
            public string LoginProvider { get; set; }
            public string ProviderKey { get; set; }
            public string UserName { get; set; }

            public IList<Claim> GetClaims()
            {
                IList<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, ProviderKey, null, LoginProvider));

                if (UserName != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, UserName, null, LoginProvider));
                }

                return claims;
            }

            public static ExternalLoginData FromIdentity(ClaimsIdentity identity)
            {
                if (identity == null)
                {
                    return null;
                }

                Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

                if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer)
                    || String.IsNullOrEmpty(providerKeyClaim.Value))
                {
                    return null;
                }

                if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
                {
                    return null;
                }

                return new ExternalLoginData
                {
                    LoginProvider = providerKeyClaim.Issuer,
                    ProviderKey = providerKeyClaim.Value,
                    UserName = identity.FindFirstValue(ClaimTypes.Name)
                };
            }
        }

        private static class RandomOAuthStateGenerator
        {
            private static RandomNumberGenerator _random = new RNGCryptoServiceProvider();

            public static string Generate(int strengthInBits)
            {
                const int bitsPerByte = 8;

                if (strengthInBits % bitsPerByte != 0)
                {
                    throw new ArgumentException("strengthInBits must be evenly divisible by 8.", "strengthInBits");
                }

                int strengthInBytes = strengthInBits / bitsPerByte;

                byte[] data = new byte[strengthInBytes];
                _random.GetBytes(data);
                return HttpServerUtility.UrlTokenEncode(data);
            }
        }

        #endregion
    }
}
