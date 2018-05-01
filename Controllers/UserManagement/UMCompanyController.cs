using Newtonsoft.Json.Linq;
using StandAloneB2B.DataAccess;
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
using System.Web.Http;
using System.Web.OData;
using System.Web.OData.Query;

namespace StandAloneB2B.Presentation.Controllers.UserManagement
{
    [RoutePrefix("api/UMCompanies")]
    public class UMCompanyController : SiteTRAXApiController<DataAccess.Models.UMCompany>
    {
        private IUMCompanyService umcompanyService;


        public UMCompanyController(IUMCompanyService umcompanyService)
        {
            this.umcompanyService = umcompanyService;
        }

        [HttpPost, Route("createorupdate")]
        public new IHttpActionResult CreateOrUpdate([FromBody] UMCompany model)
        {
            if (model == null)
            {
                return BadRequest("Company must be provided");
            }

            UMCompany company = umcompanyService.Read(model.UMCompanyK);
            if (company == null)
            {
                company = umcompanyService.Create(model);
            }
            else
            {
                umcompanyService.Update(company.UMCompanyK, model);
            }

            umcompanyService.SaveChanges();
            return Ok();
        }

        [HttpGet, Route("read")]
        public UMCompany Read(Guid key)
        {
            return umcompanyService.Read(key);
        }

        #region Update
        
        public IHttpActionResult Update(Guid UMCompanyK, [FromBody] UMCompany model)
        {
            if (model == null)
            {
                return BadRequest("Company must be provided");
            }
            umcompanyService.Update(model.UMCompanyK, model);
            umcompanyService.SaveChanges();

            return Ok();
        }

        #endregion

        #region Delete

        [HttpDelete, Route("delete/{umCompanyK}")]
        public IHttpActionResult Delete(Guid umCompanyK)
        {
            try
            {
                umcompanyService.Delete(umCompanyK);
                umcompanyService.SaveChanges();
            }
            catch (Exception)
            {
                return BadRequest("Something went wrong when deleting the company");
            }

            return Ok();
        }
        #endregion

        #region Search
        
        [HttpGet, Route("find")]
        public override IQueryable Find(string searchTerm = null)
        {
            return umcompanyService.Find(searchTerm)
                .Select(i => new
                {
                    i.UMCompanyK,
                    i.Name,
                    i.Email,
                    i.Phone,
                    i.Fax,
                    i.StreetNumber,
                    i.StreetName,
                    i.StreetType,
                    i.Suburb,
                    i.City,
                    i.PostalCode,
                    i.CreatedAtUtc,
                    i.CreatedByUserFK,
                    i.ModifiedAtUtc,
                    i.ModifiedByUserFK
                });
            
        }

        [HttpGet, Route("findpaged")]
        public PageResult<object> FindPaged(ODataQueryOptions<UMCompanyDto> options, string searchTerm = null)
        {
            var pageResult = ApplyPaging(options, umcompanyService.Find(searchTerm)
                .Select(i => new UMCompanyDto()
                {
                    UMCompanyK = i.UMCompanyK,
                    Name = i.Name,
                    Email = i.Email,
                    Phone = i.Phone,
                    Fax = i.Fax,
                    StreetNumber = i.StreetNumber,
                    StreetName = i.StreetName,
                    StreetType = i.StreetType,
                    Suburb = i.Suburb,
                    City = i.City,
                    State = "",
                    PostalCode = i.PostalCode,
                    Country = ""
                }));

            return pageResult;

        }
        #endregion

        [HttpPost, Route("addusertocompany")]
        public IHttpActionResult AddUserToCompany(UserCompanyDto model)
        {
            umcompanyService.AddUserToCompany(model.UMCompanyK, model.UserK);
            umcompanyService.SaveChanges();

            return Ok();
        }

        [HttpPost, Route("removeuserfromcompany")]
        public IHttpActionResult RemoveUserFromCompany(UserCompanyDto model)
        {
            umcompanyService.RemoveUserFromCompany(model.UMCompanyK, model.UserK);
            umcompanyService.SaveChanges();

            return Ok();
        }

        [HttpGet, Route("getuserunassignedcompanies")]
        public IQueryable GetUserUnassignedCompanies(int userK, string searchTerm = "")
        {
            return umcompanyService.GetUserUnassignedCompanies(userK, searchTerm);
        }

        [HttpPost, Route("addcompaniestouser")]
        public IHttpActionResult AddCompaniesToUser(AddCompaniesToUserDto model)
        {
            umcompanyService.AddCompaniesToUser(model.Companies, model.UserK);
            umcompanyService.SaveChanges();
            return Ok();
        }

    }
}
