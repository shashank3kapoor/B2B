using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using StandAloneB2B.DataAccess.Models;
using StandAloneB2B.Presentation.Models.BusinessManagement;
using StandAloneB2B.ServiceFramework.BusinessManagement;
using static System.DateTime;

namespace StandAloneB2B.Presentation.Controllers.BusinessManagement
{
    [RoutePrefix("api/BusinessManagement/Menus")]
    public class MenuController : Framework.SiteTRAXApiController
    {
        private IBmpMenuService menuService;

        public MenuController(IBmpMenuService menuService)
        {
            this.menuService = menuService; 

        }
        public override IQueryable Find(string searchTerm = null)
        {
            return menuService.Find(searchTerm); 
        }

        [HttpGet, Route("getMenu")]
        public IQueryable GetMenu()
        {
            return menuService.PrepareMenuTree("").AsQueryable();
        }

        [HttpGet, Route("getMenu/{menuName}")]
        public IQueryable GetMenu(string menuName)
        {
            return menuService.PrepareMenuTree(menuName).AsQueryable();
        }

        [HttpPost, Route("new")]
        public IHttpActionResult CreateMenu([FromBody]MenuCreateDto model)
        {
            //step 1 : validate our incoming model (every asp .net controller will be same)
            if (ModelState.IsValid == false)
            {
                return BadRequest(ModelState); //ModelState is built in asp .net. 
            }

            //step 2 : pass model to service framework to create in the db 

            BmpMenu newobj = new BmpMenu() { MenuName = model.MenuName ,
                                        MenuOrder = model.MenuOrder ,
                                        ParentMenuID = model.ParentMenuID ,
                                        UiSref = model.UiSref ,
                                        UiSrefActiveIf = model.UiSrefActiveIf ,
                                        MenuLevel = model.MenuLevel
                                        };

            menuService.Create(newobj);

            //step 3 : call save changes 
            try
            {
                menuService.SaveChanges();
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
            

            return Ok("all good : "+ newobj.MenuName + " has been created."); 
        }
    }
}
