using System.Linq;
using System.Web.Http;
using StandAloneB2B.DataAccess.Models;
using StandAloneB2B.Presentation.Models;
using StandAloneB2B.ServiceFramework.Identity;

namespace StandAloneB2B.Presentation.Controllers.Identity
{
    [RoutePrefix("api/Roles")]
    public class RolesController : Framework.SiteTRAXApiController<DataAccess.Models.Role>
    {
        private IRoleService roleService;

        public RolesController(IRoleService roleService)
        {
            this.roleService = roleService;
        }

        [HttpPost, Route("new")]
        public IHttpActionResult CreateRole([FromBody] RoleCreateDto model)
        {
            Role newRole;

            if (ModelState.IsValid == false)
            {
                return BadRequest(ModelState);
            }

            newRole = roleService.Create(model.RoleName);
            roleService.SaveChanges();

            return Ok(newRole);
        }

        [HttpGet, Route("delete")]
        public IHttpActionResult DeleteRole(int RoleK)
        {
            if (ModelState.IsValid == false)
            {
                return BadRequest(ModelState);
            }
            roleService.Delete(RoleK);
            
            return Ok();
        }

        //public override IQueryable Find(string searchTerm = null)
        //{
        //    var qRoles = roleService.Find(searchTerm);

        //    return qRoles;
        //}

        [HttpGet, Route("find")]
        public override IQueryable Find(string searchTerm = null)
        {
            var qRoles = roleService.Find(searchTerm);

            return qRoles;
        }
    }
}
