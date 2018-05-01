using StandAloneB2B.DataAccess.Models;
using StandAloneB2B.Presentation.Framework;
using StandAloneB2B.ServiceFramework.UserAdministration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace StandAloneB2B.Presentation.Controllers.UserManagement
{
    [RoutePrefix("api/PermissionTypes")]
    public class PermissionTypeController : SiteTRAXApiController<PermissionType>
    {
        private IPermissionTypeService permissionService;

        public PermissionTypeController(IPermissionTypeService permissionService)
        {
            this.permissionService = permissionService;
        }

        [HttpPost, Route("createorupdate")]
        public new IHttpActionResult CreateOrUpdate([FromBody] PermissionType model)
        {
            if (model == null)
            {
                return BadRequest("Permission Type must be provided");
            }

            var permission = permissionService.Read(model.PermissionTypeK);
            if (permission == null)
            {
                permission = permissionService.Create(model);
            }
            else
            {
                permissionService.Update(permission.PermissionTypeK, model);
            }

            return Ok();
        }

        [HttpGet, Route("read")]
        public SingleResult<PermissionType> Read(Guid key)
        {
            return SingleResult.Create(permissionService.Find(i => i.PermissionTypeK == key));
        }

        #region Update

        public IHttpActionResult Update(Guid GroupK, [FromBody] PermissionType model)
        {
            if (model == null)
            {
                return BadRequest("Group must be provided");
            }

            //UMAddress address = umaddressService.Read(UMAddressK);

            //umaddressService.SaveChanges();s

            permissionService.Update(model.PermissionTypeK, model);

            return Ok();
        }

        #endregion

        #region Delete

        public IHttpActionResult Delete(Guid GroupK)
        {
            permissionService.Delete(GroupK);
            permissionService.SaveChanges();

            return Ok();
        }
        #endregion

        #region Search

        [HttpGet, Route("find")]
        public override IQueryable Find(string searchTerm = null)
        {
            return permissionService.Find(searchTerm)
                .Select(i => new
                {
                    i.PermissionTypeK,
                    i.Name,
                    i.CreatedAtUtc,
                    i.CreatedByUserFK,
                    i.ModifiedAtUtc,
                    i.ModifiedByUserFK
                });
        }
        #endregion
    }
}
