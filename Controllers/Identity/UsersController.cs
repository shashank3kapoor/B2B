using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using StandAloneB2B.DataAccess.Models;
using StandAloneB2B.Presentation.Framework;
using StandAloneB2B.Presentation.Models;
using StandAloneB2B.ServiceFramework;
using StandAloneB2B.ServiceFramework.Identity;
using StandAloneB2B.ServiceFramework.Inventory;
using StandAloneB2B.Presentation.Models.UserManagement;
using StandAloneB2B.Common.Email;
using StandAloneB2B.ServiceFramework.UserAdministration;
using System.Collections;
using System.Collections.Generic;
using System.Web.OData;
using System.Web.OData.Query;

namespace StandAloneB2B.Presentation.Controllers.Identity
{
    [Authorize]
    [RoutePrefix("api/Users")]
    public class UsersController : SiteTRAXApiController
    {
        private IUserService userRepo;
        private ICompanyService companyRepo;
        private IUMCompanyService umCompanyRepo;
        private IUserCodeService userCodeRepo;
        private IGroupService groupService;
        private IPermissionService permissionService;
        private IAdministrationSecurityService adminSecurityService;
        
        public UsersController(IUserService userRepo, ICompanyService companyRepo, IUserCodeService userCodeRepo, IGroupService groupService, IPermissionService permissionService, IUMCompanyService umCompanyRepo, IAdministrationSecurityService adminSecurityService)
        {
            this.userRepo = userRepo;
            this.companyRepo = companyRepo;
            this.userCodeRepo = userCodeRepo;
            this.groupService = groupService;
            this.permissionService = permissionService;
            this.umCompanyRepo = umCompanyRepo;
            this.adminSecurityService = adminSecurityService;
        }
        
        [HttpPost, Route("")]
        public async Task<IHttpActionResult> CreateUserAsync(UserCreateDto model)
        {
            User newUser;

            if (model == null)
            {
                ModelState.AddModelError("model", new ArgumentNullException(nameof(model)));
            }
            model.RepeatPassword = model.NewPassword;

            model.Roles.Add("deliverypartner");

            if (ModelState.IsValid == false)
            {
                return BadRequest(ModelState);
            }
            
            newUser = await userRepo.CreateAsync(model.Email, model.Email, model.FirstName, 
                model.NewPassword, 0, model.LastName, model.CompanyFK);

            #region Add user to company
            umCompanyRepo.AddUserToCompanies(model.UserCompanies, newUser.Id);
            umCompanyRepo.SaveChanges();
            #endregion

            #region Send one time code for initial change password
            var userCode = userCodeRepo.Create(new UserCode() { UserK = newUser.Id });
            userCodeRepo.SaveChanges();
            string code = userCode.Code;

            var adminSecurity = adminSecurityService.GetAdministrationSecurity();
            EmailDto emaildto = new EmailDto()
            {
                EmailBody = String.Format("Hi {0} {1}. You have been added as a new user to siteTRAX Evolution. <br/><br/> Your Onetime code is: <b>{2}</b> <br/> This Onetime code is valid until: <b>{3}</b> at which time it will expire and a new one code will be required to be requested. <br/><br/> To enter your onetime code. Click on \"Forget my password\" then click on \"I have a onetime code\" <br/><br/>If you did not request this password reset, please ignore this message. <br/> Do not reply to this email message as the mail box is un-monitored.", newUser.FirstName, newUser.LastName, userCode.Code, userCode.ExpirationDateUtc.ToLocalTime().ToString("dd-MMMM-yyyy hh:mm tt") ),
                EmailSubject = "New User - siteTRAX Evolution",
                EmailSender = "noreplay.StandAloneB2B.evo@gmail.com",
                EmailRecipient = newUser.Email
            };

            CustomEmail.SendPasswordEmail(adminSecurity.MailerServer, adminSecurity.MailerServerPort.Value, adminSecurity.MailerUsername, adminSecurity.MailerPassword, adminSecurity.PasswordResetEmail, newUser.Email, emaildto.EmailSubject, emaildto.EmailBody);
            #endregion



            //await userRepo.AssignRolesAsync(newUser, model.Roles.ToArray());

            userRepo.SaveChanges();

            return Ok(newUser);
        }

        [HttpGet, Route("{id}")]
        public UserDto Read(int id)
        {
            var qUser = userRepo
                .Find(i => i.Id == id)
                .Include(i => i.Roles/*.Select(x => x.Name)*/).FirstOrDefault();

            if(qUser != null)
            {
                return new UserDto()
                {
                    //CompanyFK = qUser.CompanyFK,
                    Email = qUser.Email,
                    FirstName = qUser.FirstName,
                    LastName = qUser.LastName,
                    Id = qUser.Id,
                    PhoneNumber = qUser.PhoneNumber,
                    MobileNumber = qUser.MobileNumber,                   
                    PersonalEmailAddress = qUser.PersonalEmailAddress,
                    NbnCardNumber = qUser.NbnCardNumber,
                    IsUCGListed = qUser.IsUCGListed,
                    StreetName = qUser.StreetName,
                    StreetNumber = qUser.StreetNumber,
                    StreetType = qUser.StreetType,
                    Suburb = qUser.Suburb,
                    City = qUser.City,
                    PostalCode = qUser.PostalCode,
                    StateFK = qUser.StateFK,
                    CountryFK = qUser.CountryFK,
                    EmergencyContactName = qUser.EmergencyContactName,
                    EmergencyContactNumbers = qUser.EmergencyContactNumbers,
                    ConnectUsername = qUser.ConnectUsername,
                    ConnectPassword = qUser.ConnectPassword,
                    Roles = qUser.Roles.Select(x => x.Name).ToList(),
                    AccountLocked = qUser.LockoutEnabled,
                    LoginOption = qUser.LoginOption.Value,
                    Status = ((Status)qUser.Status).ToString(),
                    LastLogin = qUser.LastLoginDateUtc,
                    EffectivePermission = "",
                    UserGroups = userRepo.FindUserGroups(qUser.Id).ToList(),
                    UserPermissions = userRepo.FindUserPermissions(qUser.Id).ToList(),
                    UserCompanies = userRepo.FindUserCompanies(qUser.Id).ToList().Select(i => new UserCompanyLinkDto()
                    {
                        UMCompanyK = i.UMCompany.UMCompanyK,
                        CompanyName = i.UMCompany.Name,
                        StreetName = i.UMCompany.StreetName,
                        StreetNumber = i.UMCompany.StreetNumber,
                        StreetType = i.UMCompany.StreetType,
                        Suburb = i.UMCompany.Suburb,
                        City = i.UMCompany.City,
                        PostalCode = i.UMCompany.PostalCode,
                    }).ToList(),
                    IsDeleted = qUser.IsDeleted
                };
            }

            return null;
                
        }

        [HttpPut, Route("{id}")]
        public async Task<IHttpActionResult> UpdateUserAsync(int id, [FromBody] UserUpdateDto model)
        {
            
            var user = await UserManager.FindByIdAsync(id);
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.UserName = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.MobileNumber = model.MobileNumber;            
            user.PersonalEmailAddress = model.PersonalEmailAddress;
            user.IsUCGListed = model.IsUCGListed;
            user.NbnCardNumber = model.NbnCardNumber;
            user.EmergencyContactName = model.EmergencyContactName;
            user.EmergencyContactNumbers = model.EmergencyContactNumbers;
            user.ConnectPassword = model.ConnectPassword;
            user.ConnectUsername = model.ConnectUsername;
            user.LoginOption = model.LoginOption;
            user.StreetType = model.StreetType;
            user.StreetName = model.StreetName;
            user.StreetNumber = model.StreetNumber;
            user.Suburb = model.Suburb;
            user.City = model.City;
            user.PostalCode = model.PostalCode;
            user.StateFK = model.StateFK;
            user.CountryFK = model.CountryFK;
            var updateResult = await UserManager.UpdateAsync(user);
            
            return Ok(model);
        }

        [HttpDelete, Route("delete/{id}")]
        public async Task<IHttpActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await UserManager.FindByIdAsync(id);
                user.IsDeleted = true;
                var updateResult = await UserManager.UpdateAsync(user);
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong when deleting the user");
            }

            return Ok();
        }

        [HttpPut, Route("undelete/{id}")]
        public async Task<IHttpActionResult> UnDeleteUser(int id)
        {
            try
            {
                var user = await UserManager.FindByIdAsync(id);
                user.IsDeleted = false;
                var updateResult = await UserManager.UpdateAsync(user);
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong when undeleting the user");
            }

            return Ok();
        }

        [HttpPost, Route("ChangePassword")]
        public IHttpActionResult ChangePassword([FromBody] UserChangePasswordDto model)
        {
            if (ModelState.IsValid == false)
            {
                return BadRequest(ModelState);
            }

            userRepo.ChangePassword(UserK, model.CurrentPassword, model.NewPassword);

            userRepo.SaveChanges();

            return Ok();
        }

        [HttpGet, Route("")]
        public override IQueryable Find(string searchTerm = null)
        {
            return userRepo
                .Find(searchTerm)
                .Select(i => new UserDto
                {
                    Id = i.Id,
                    //CompanyFK = i.CompanyFK,
                    Email = i.Email,
                    FirstName = i.FirstName,
                    LastName = i.LastName,
                    PhoneNumber = i.PhoneNumber,
                    UserName = i.UserName,
                    //CompanyName = i.UMCompany.Name,
                    AccountLocked = i.LockoutEnabled,
                    LastLogin = i.LastLoginDateUtc,
                    Status = ((Status)i.Status).ToString(),
                });
        }

        [HttpGet, Route("FindWithStatus")]
        public PageResult<object> FindWithStatus(ODataQueryOptions<UserDto> options,  string searchTerm = null, string status = "All", bool isDeleted = false)
        {
            if(status == "All")
            {
                return ApplyPaging(options, userRepo
               .Find(searchTerm).Where(i => i.IsDeleted == isDeleted)
               .Select(i => new UserDto
               {
                   Id = i.Id,
                   //CompanyFK = i.CompanyFK,
                   Email = i.Email,
                   FirstName = i.FirstName,
                   LastName = i.LastName,
                   PhoneNumber = i.PhoneNumber,
                   UserName = i.UserName,
                   //CompanyName = i.UMCompany != null ? i.UMCompany.Name : "",
                   AccountLocked = i.LockoutEnabled,
                   LastLogin = i.LastLoginDateUtc,
                   Status = ((Status)i.Status).ToString()
               }));
            }
            else
            {
                var statusInt = (int)(Status)Enum.Parse(typeof(Status), status);
                return ApplyPaging(options, userRepo
                .Find(searchTerm, i => i.Status == statusInt).Where(i => i.IsDeleted == isDeleted)
                  .Select(i => new UserDto
                  {
                      Id = i.Id,
                      //CompanyFK = i.CompanyFK,
                      Email = i.Email,
                      FirstName = i.FirstName,
                      LastName = i.LastName,
                      PhoneNumber = i.PhoneNumber,
                      UserName = i.UserName,
                      //CompanyName = i.UMCompany != null ? i.UMCompany.Name : "",
                      AccountLocked = i.LockoutEnabled,
                      LastLogin = i.LastLoginDateUtc,
                      Status = ((Status)i.Status).ToString()
                  }));
                
            }
           
        }

        [HttpGet, Route("finduserswithnogroups")]
        public IQueryable FindUsersWithNoGroups(Guid groupK)
        {
            return userRepo
                .FindUsersWithNoGroups(groupK)
                .Select(i => new UserDto
                {
                    Id = i.Id,
                    FirstName = i.FirstName,
                    LastName = i.LastName,
                });
            
        }

        [HttpGet, Route("finduserswithnopermissions")]
        public IQueryable FindUsersWithNoPermissions(Guid permissionK)
        {
            return userRepo
                .FindUsersWithNoPermissions(permissionK)
                .Select(i => new UserDto
                {
                    Id = i.Id,
                    FirstName = i.FirstName,
                    LastName = i.LastName,
                });

        }

        [HttpPut, Route("activate/{userK}")]
        public async Task<IHttpActionResult> Activate(int userK)
        {
            var user = await UserManager.FindByIdAsync(userK);

            if (user == null)
                return BadRequest("User not found");

            user.Status = (int)Status.Active;
            await UserManager.UpdateAsync(user);

            return Ok();
        }

        [HttpPut, Route("suspend/{userK}")]
        public async Task<IHttpActionResult> Suspend(int userK)
        {
            var user = await UserManager.FindByIdAsync(userK);

            if (user == null)
                return BadRequest("User not found");

            user.Status = (int)Status.Suspended;
            await UserManager.UpdateAsync(user);

            return Ok();
        }

        [HttpPut, Route("disable/{userK}")]
        public async Task<IHttpActionResult> Disable(int userK)
        {
            var user = await UserManager.FindByIdAsync(userK);

            if (user == null)
                return BadRequest("User not found");

            user.Status = (int)Status.Disabled;
            await UserManager.UpdateAsync(user);

            return Ok();
        }

        [HttpPut, Route("unlock/{userK}")]
        public async Task<IHttpActionResult> Unlock(int userK)
        {
            var user = await UserManager.FindByIdAsync(userK);

            if (user == null)
                return BadRequest("User not found");

            user.Status = (int)Status.Active;
            user.AccessFailedCount = 0;
            user.LockoutEndDateUtc = null;
            await UserManager.UpdateAsync(user);

            return Ok();
        }

        [HttpPost, Route("changelogin")]
        public async Task<IHttpActionResult> ChangeLogin(UserDto model)
        {
            var user = await UserManager.FindByIdAsync(model.Id.Value);
            user.LoginOption = model.LoginOption;
            await UserManager.UpdateAsync(user);

            return Ok();
        }


        [HttpGet, Route("findusereffectivepermissions")]
        public List<UserEffectivePermissionDto> FindUserEffectivePermissions(int userK)
        {
            var userGroupPermissions = userRepo.FindUserGroupsPermissions(userK);
            var userPermissions = userRepo.FindUserPermissions(userK);

            var totalPermissions = new List<Permission>();
            totalPermissions.AddRange(userPermissions);
            totalPermissions.AddRange(userGroupPermissions);

            totalPermissions = totalPermissions.Distinct().ToList();

            var userEffectivePermissions = new List<UserEffectivePermissionDto>();
            foreach (var permission in totalPermissions.OrderBy(i => i.Name))
            {
                var userEffectivePermission = new UserEffectivePermissionDto();
                userEffectivePermission.PermissionK = permission.PermissionK;
                userEffectivePermission.PermissionGroupingName = permission.GroupingName;
                userEffectivePermission.UserPermission = "";
                userEffectivePermission.GroupPermission = "";
                userEffectivePermission.PermissioName = permission.Name;
                userEffectivePermission.PermissioDescription = permission.Description;
                
                userEffectivePermissions.Add(userEffectivePermission);
            }

            return userEffectivePermissions;
        }

        [HttpGet, Route("findusercertificates")]
        public IQueryable FindUserCertificates(int userK, string selectedDeleted)
        {
            var certificates = userRepo.FindUserCertificates(userK).Select(i => new UserCertificationsDto()
            {
                UserCertificationK = i.UserCertificateK,
                StartDate = i.StartDate,
                EndDate = i.EndDate,
                ExpiryWarningPeriodInDays = i.ExpiryWarningPeriodInDays,
                ApprovalStatus = i.UserCertificateHistories.OrderByDescending(j => j.DateUploaded).FirstOrDefault() == null  ? "No files uploaded" : (i.UserCertificateHistories.OrderByDescending(j => j.DateUploaded).FirstOrDefault().IsApproved ? "Approved" : "Unapproved"),
                FilePath = i.UserCertificateHistories.OrderByDescending(j => j.DateUploaded).FirstOrDefault().FilePath,
                IsDeleted = i.IsDeleted
            });

            if (selectedDeleted == "0")
                certificates = certificates.Where(i => !i.IsDeleted);
            else if (selectedDeleted == "1")
                certificates = certificates.Where(i => i.IsDeleted);

            return certificates;
        }

    }
}