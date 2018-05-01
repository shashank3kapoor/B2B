using StandAloneB2B.DataAccess.Models;
using StandAloneB2B.Presentation.Framework;
using StandAloneB2B.Presentation.Models.UserManagement;
using StandAloneB2B.ServiceFramework.UserAdministration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Query;

namespace StandAloneB2B.Presentation.Controllers.UserManagement
{
    [RoutePrefix("api/Permissions")]
    public class PermissionController : SiteTRAXApiController<Permission>
    {
        private IPermissionService permissionService;

        public PermissionController(IPermissionService permissionService)
        {
            this.permissionService = permissionService;
        }

        [HttpPost, Route("createorupdate")]
        public override IHttpActionResult CreateOrUpdate([FromBody] Permission model)
        {
            if (model == null)
            {
                return BadRequest("Permission must be provided");
            }

            var permission = permissionService.Read(model.PermissionK);
            if (permission == null)
            {
                permission = permissionService.Create(model);
            }
            else
            {
                permissionService.Update(permission.PermissionK, model);
            }

            permissionService.SaveChanges();

            return Ok();
        }

        [HttpGet, Route("read")]
        public SingleResult<Permission> Read(Guid permissionK)
        {
            return SingleResult.Create(permissionService.Find(i => i.IsDeleted == false && i.PermissionK == permissionK));
        }

        [HttpGet, Route("getByName")]
        public SingleResult<Permission> getByName(string permission_name)
        {

            return SingleResult.Create(permissionService.Find(i => i.IsDeleted == false && i.Name.Equals(permission_name)));
        }

        #region Update

        public IHttpActionResult Update(Guid PermissionK, [FromBody] Permission model)
        {
            if (model == null)
            {
                return BadRequest("Permission must be provided");
            }
            
            permissionService.Update(model.PermissionK, model);
            permissionService.SaveChanges();

            return Ok();
        }

        #endregion

        #region Delete
        [HttpDelete, Route("delete/{permissionK}")]

        public IHttpActionResult Delete(Guid permissionK)
        {
            permissionService.Delete(permissionK);
            permissionService.SaveChanges();

            return Ok();
        }
        #endregion

        #region Search

        [HttpGet, Route("find")]
        public override IQueryable Find(string searchTerm = null)
        {
            return permissionService.Find(searchTerm).OrderBy(i => i.Identity)
                .Select(i => new PermissionDto()
                {
                    PermissionK = i.PermissionK,
                    Name = i.Name,
                    GroupingName = i.GroupingName,
                    Description = i.Description,
                    IsEnabled = i.IsEnabled
                });
        }

        [HttpGet, Route("findenabledpermissions")]
        public IQueryable FindEnabledPermissions()
        {
            return permissionService.Find("").Where(i => i.IsEnabled)
                .Select(i => new PermissionDto()
                {
                    PermissionK = i.PermissionK,
                    Name = i.Name,
                    GroupingName = i.GroupingName,
                    Description = i.Description,
                    IsEnabled = i.IsEnabled
                });
        }

        [HttpGet, Route("findpaged")]
        public PageResult<object> FindPaged(ODataQueryOptions<PermissionDto> options, string searchTerm = null)
        {
            return ApplyPaging(options, permissionService.Find(searchTerm).OrderBy(i => i.Identity)
                .Select(i => new PermissionDto()
                {
                    PermissionK = i.PermissionK,
                    Name = i.Name,
                    GroupingName = i.GroupingName,
                    Description = i.Description,
                    IsEnabled = i.IsEnabled
                }));
        }

        [HttpGet, Route("findwithstatuspaged")]
        public PageResult<object> FindWithStatusPaged(ODataQueryOptions<PermissionDto> options, string searchTerm = null, string status = "All")
        {
            if (status == "All")
            {
                return ApplyPaging(options, permissionService.Find(searchTerm).OrderBy(i => i.Identity)
                .Select(i => new PermissionDto()
                {
                    PermissionK = i.PermissionK,
                    Identity = i.Identity,
                    Name = i.Name,
                    GroupingName = i.GroupingName,
                    Description = i.Description,
                    IsEnabled = i.IsEnabled
                }));
            }
            else
            {
                return ApplyPaging(options, permissionService.Find(searchTerm).Where(a => a.IsEnabled == (status == "Enabled" ? true: false))
                .Select(i => new PermissionDto()
                {
                    PermissionK = i.PermissionK,
                    Identity = i.Identity,
                    Name = i.Name,
                    GroupingName = i.GroupingName,
                    Description = i.Description,
                    IsEnabled = i.IsEnabled
                }));
            }
        }
        
        [HttpGet, Route("findpermissionusers")]
        public IQueryable FindPermissionUsers(Guid permissionK)
        {
            return permissionService.FindPermissionUsers(permissionK)
                .Select(i => new
                {
                    i.User.Id,
                    i.User.FirstName,
                    i.User.LastName
                });
        }

        [HttpGet, Route("findpermissiongroups")]
        public IQueryable FindPermissionGroups(Guid permissionK)
        {
            return permissionService.FindPermissionGroups(permissionK)
                .Select(i => new
                {
                    i.Group.GroupK,
                    i.Group.Name
                });
        }
        [HttpGet, Route("findgroupunassignedpermissions")]
        public IQueryable FindGroupUnassignedPermissions(Guid groupK, string searchTerm = "")
        {
            return permissionService.FindGroupUnassignedPermissions(groupK, searchTerm);
        }

        [HttpGet, Route("finduserunassignedpermissions")]
        public IQueryable FindUserUnassignedPermissions(int userK, string searchTerm = "")
        {
            return permissionService.FindUserUnassignedPermissions(userK, searchTerm);
        }
        
        #endregion

        [HttpPost, Route("adduserstopermission")]
        public IHttpActionResult AddUsersToPermission(AddPermissionUserDto model)
        {
            permissionService.AddUsersToPermission(model.Users, model.PermissionK);
            permissionService.SaveChanges();
            return Ok();
        }

        [HttpPost, Route("addgroupstopermission")]
        public IHttpActionResult AddGroupsToPermission(AddPermissionGroupDto model)
        {
            permissionService.AddGroupsToPermission(model.Groups, model.PermissionK);
            permissionService.SaveChanges();

            return Ok();
        }

        [HttpPost, Route("addpermissionstogroup")]
        public IHttpActionResult AddPermissionsToGroup(AddPermissionsToGroup model)
        {
            permissionService.AddPermissionsToGroup(model.PermissionKeys, model.GroupK);
            permissionService.SaveChanges();

            return Ok();
        }

        [HttpPost, Route("addpermissionstouser")]
        public IHttpActionResult AddPermissionsToUser(AddPermissionsToUser model)
        {
            permissionService.AddPermissionsToUser(model.PermissionKeys, model.UserK);
            permissionService.SaveChanges();

            return Ok();
        }

        [HttpPost, Route("removepermissionfromgroup")]
        public IHttpActionResult RemovePermissionFromGroup(GroupPermissionDto model)
        {
            permissionService.RemovePermissionFromGroup(model.PermissionK, model.GroupK);
            permissionService.SaveChanges();

            return Ok();
        }

        [HttpPost, Route("removepermissionfromuser")]
        public IHttpActionResult RemovePermissionFromUser(UserPermissionDto model)
        {
            permissionService.RemovePermissionFromUser(model.PermissionK, model.UserK);
            permissionService.SaveChanges();

            return Ok();
        }

        [HttpPut, Route("enable/{permissionK}")]
        public IHttpActionResult Enable(Guid permissionK)
        {
            try
            {
                permissionService.Enable(permissionK);
                permissionService.SaveChanges();
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong when enabing the permission");
            }

            return Ok();

        }

        [HttpPut, Route("disable/{permissionK}")]
        public IHttpActionResult Disable(Guid permissionK)
        {
            try
            {
                permissionService.Disable(permissionK);
                permissionService.SaveChanges();
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong when disabling the permission");
            }

            return Ok();

        }
    }
}
