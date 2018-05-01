using StandAloneB2B.DataAccess.Models;
using StandAloneB2B.Presentation.Framework;
using StandAloneB2B.Presentation.Models.UserManagement;
using StandAloneB2B.ServiceFramework.UserAdministration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Query;

namespace StandAloneB2B.Presentation.Controllers.UserManagement
{
    [RoutePrefix("api/Groups")]
    public class GroupController : SiteTRAXApiController<Group>
    {
        private IGroupService groupService;
        private IPermissionService permissionService;

        public GroupController(IGroupService groupService, IPermissionService permissionService)
        {
            this.groupService = groupService;
            this.permissionService = permissionService;
        }

        [HttpPost, Route("createorupdate")]
        public override IHttpActionResult CreateOrUpdate([FromBody] Group model)
        {
            if (model == null)
            {
                return BadRequest("Group must be provided");
            }
            
            var group = groupService.Read(model.GroupK);
            if (group == null)
            {
                group = groupService.Create(model);
            }
            else
            {
                groupService.Update(group.GroupK, model);
            }

            groupService.SaveChanges();


            return Ok();
        }

        [HttpGet, Route("read")]
        public GroupDto Read(Guid groupK)
        {
            var group = groupService.Find(i => i.IsDeleted == false && i.GroupK == groupK).FirstOrDefault();
            return new GroupDto()
            {
                Name = group.Name,
                GroupK = group.GroupK,
                Description = group.Description,
                Status = ((Status)group.Status).ToString(),
            };


        }

        #region Update

        public IHttpActionResult Update(Guid GroupK, [FromBody] Group model)
        {
            if (model == null)
            {
                return BadRequest("Group must be provided");
            }
            
            groupService.Update(model.GroupK, model);
            groupService.SaveChanges();

            return Ok();
        }

        #endregion

        #region Delete
        [HttpDelete, Route("delete/{groupK}")]
        public IHttpActionResult Delete(Guid groupK)
        {
            try
            {
                groupService.Delete(groupK);
                groupService.SaveChanges();
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong when deleting this group.");
            }
            return Ok();
        }
        #endregion

        #region Search

        [HttpGet, Route("find")]
        public override IQueryable Find(string searchTerm = null)
        {
            return groupService.Find(searchTerm)
                .Select(i => new GroupDto()
                {
                    GroupK = i.GroupK,
                    Name = i.Name,
                    Status = ((Status)i.Status).ToString(),
                    Description = i.Description
                });
        }

        [HttpGet, Route("findpaged")]
        public PageResult<object> FindPaged(ODataQueryOptions<GroupDto> options, string searchTerm = null)
        {
            return ApplyPaging(options, groupService.Find(searchTerm)
                .Select(i => new GroupDto()
                {
                    GroupK = i.GroupK,
                    Name = i.Name,
                    Status = ((Status)i.Status).ToString(),
                    Description = i.Description
                }));
        }

        [HttpGet, Route("FindWithStatus")]
        public IQueryable FindWithStatus(string searchTerm = null, string status = "All")
        {
            if (status == "All")
            {
                return groupService
               .Find(searchTerm)
               .Select(i => new GroupDto()
               {
                   GroupK = i.GroupK,
                   Name = i.Name,
                   Status = ((Status)i.Status).ToString(),
                   Description = i.Description
               });
            }
            else
            {
                var statusInt = (int)(Status)Enum.Parse(typeof(Status), status);
                return groupService
               .Find(searchTerm, i => i.Status == statusInt)
               .Select(i => new GroupDto()
               {
                   GroupK = i.GroupK,
                   Name = i.Name,
                   Status = ((Status)i.Status).ToString(),
                   Description = i.Description
               });

            }

        }

        [HttpGet, Route("findwithstatuspaged")]
        public PageResult<object> FindWithStatusPaged(ODataQueryOptions<GroupDto> options, string searchTerm = null, string status = "All")
        {
            if (status == "All")
            {
                return ApplyPaging(options, groupService
               .Find(searchTerm)
               .Select(i => new GroupDto()
               {
                   GroupK = i.GroupK,
                   Name = i.Name,
                   Status = ((Status)i.Status).ToString(),
                   Description = i.Description,
                   Parent = i.Group2.Name
               }));
            }
            else
            {
                var statusInt = (int)(Status)Enum.Parse(typeof(Status), status);
                return ApplyPaging(options, groupService
               .Find(searchTerm, i => i.Status == statusInt)
               .Select(i => new GroupDto()
               {
                   GroupK = i.GroupK,
                   Name = i.Name,
                   Status = ((Status)i.Status).ToString(),
                   Description = i.Description,
                   Parent = i.Group2.Name
               }));

            }

        }

        [HttpGet, Route("findgroupusers")]
        public IQueryable FindGroupUsers(Guid groupK)
        {
            return groupService.FindGroupUsers(groupK)
                .Select(i => new
                {
                    i.User.Id,
                    i.User.FirstName,
                    i.User.LastName
                });
        }
        
        [HttpGet, Route("findgroupswithnopermissions")]
        public IQueryable FindGroupsWithNoPermission(Guid permissionK)
        {
           return groupService.FindGroupsWithNoPermission(permissionK)
                .Select(i => new
                {
                    i.GroupK,
                    i.Name
                });

        }

        [HttpGet, Route("finduserunassignedgroups")]
        public IQueryable FindUserUnassignedGroups(int userK, string searchTerm = "")
        {
            return groupService.FindUserUnassignedGroups(userK, searchTerm);

        }

        [HttpGet, Route("findexcludegroupwithoutparents")]
        public IQueryable FindExcludeGroupWithoutParents(Guid groupK, string searchTerm = "")
        {
            return groupService.FindExcludeGroupWithoutParents(groupK, searchTerm);

        }

        [HttpGet, Route("findgrouppermissions")]
        public IList<Permission> FindGroupPermissions(Guid groupK)
        {
            return groupService.FindGroupPermissions(groupK);

        }

        [HttpGet, Route("findchildgroups")]
        public IList<Group> FindChildGroups(Guid groupK)
        {
            return groupService.FindChildGroups(groupK);
        }

        
        [HttpPost, Route("addgroupstogroup")]
        public IHttpActionResult AddGroupsToGroup(AddGroupsToGroupDto model)
        {
            groupService.AddGroupsToGroup(model.GroupKeys, model.GroupK);
            groupService.SaveChanges();

            return Ok();
        }

        [HttpDelete, Route("removegroupfromparent/{childGroupK}")]
        public IHttpActionResult RemoveGroupFromParent(Guid childGroupK)
        {
            groupService.RemoveGroupFromParent(childGroupK);
            groupService.SaveChanges();

            return Ok();
        }



        #endregion
        [HttpPost, Route("addgroupstouser")]
        public IHttpActionResult AddGroupsToUser(AddGroupsToUserDto model)
        {
            groupService.AddGroupsToUser(model.GroupKeys, model.UserK);
            groupService.SaveChanges();
            return Ok();
        }

        [HttpPost, Route("removegroupfromuser")]
        public IHttpActionResult RemoveGroupFromUser(UserGroupDto model)
        {
            groupService.RemoveGroupFromUser(model.GroupK, model.UserK);
            groupService.SaveChanges();
            
            return Ok();
        }

        [HttpPost, Route("adduserstogroup")]
        public IHttpActionResult AddUsersToGroup(AddGroupUserDto model)
        {
            groupService.AddUsersToGroup(model.Users, model.GroupK);
            groupService.SaveChanges();

            return Ok();
        }

        [HttpPut, Route("activate/{groupK}")]
        public IHttpActionResult Activate(Guid groupK)
        {
            try
            {
                groupService.Activate(groupK);
                groupService.SaveChanges();
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong when activating the group");
            }

            return Ok();

        }

        [HttpPut, Route("disable/{groupK}")]
        public IHttpActionResult Disable(Guid groupK)
        {
            try
            {
                groupService.Disable(groupK);
                groupService.SaveChanges();
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong when disabling the group");
            }

            return Ok();

        }
    }
}
