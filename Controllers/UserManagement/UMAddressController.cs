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
    [RoutePrefix("api/UMAddresses")]
    public class UMAddressController : SiteTRAXApiController<UMAddress>
    {
        //private IUMAddressService umaddressService;
        //private IUMCompanyService companyService;

        //public UMAddressController(IUMAddressService umaddressService, IUMCompanyService companyService)
        //{
        //    this.umaddressService = umaddressService;
        //    this.companyService = companyService;
        //}

        //[HttpPost, Route("createorupdate")]
        //public new IHttpActionResult CreateOrUpdate([FromBody] UMAddress model)
        //{
        //    if (model == null)
        //    {
        //        return BadRequest("Address must be provided");
        //    }

        //    UMAddress address = umaddressService.Read(model.UMAddressK);
        //    if (address == null)
        //    {
        //        address = umaddressService.Create(model);
        //    }
        //    else
        //    {
        //        umaddressService.Update(address.UMAddressK, model);
        //    }
            
        //    return Ok();
        //}

        //[HttpGet, Route("read")]
        //public SingleResult<UMAddress> Read(Guid key)
        //{
        //    return SingleResult.Create(umaddressService.Find(i => i.IsDeleted == false && i.UMAddressK == key));
        //}

        //#region Update

        //public IHttpActionResult Update(Guid UMAddressK, [FromBody] UMAddress model)
        //{
        //    if (model == null)
        //    {
        //        return BadRequest("Address must be provided");
        //    }

        //    //UMAddress address = umaddressService.Read(UMAddressK);

        //    //umaddressService.SaveChanges();s

        //    umaddressService.Update(model.UMAddressK, model);

        //    return Ok();
        //}

        //#endregion

        //#region Delete

        //[HttpDelete, Route("delete/{umAddressK}")]
        //public IHttpActionResult Delete(Guid umAddressK)
        //{
        //    var isLinked = companyService.Find("").Where(i => i.AddressFK == umAddressK).Count() > 0;

        //    if(isLinked)
        //        return BadRequest("Cannot delete address. A company is currently using this address.");

        //    try
        //    {

        //        umaddressService.Delete(umAddressK);
        //    }
        //    catch (Exception)
        //    {
        //        return BadRequest("Something went wrong when deleting the address");
        //    }

        //    return Ok();
        //}
        //#endregion

        //#region Search

        //[HttpGet, Route("find")]
        //public override IQueryable Find(string searchTerm = null)
        //{
        //    return umaddressService.Find(searchTerm)
        //        .Select(i => new
        //        {
        //            i.UMAddressK,
        //            i.StreetAddress1,
        //            i.StreetAddress2,
        //            i.City,
        //            i.State,
        //            i.PostalCode,
        //            i.CreatedAtUtc,
        //            i.CreatedByUserFK,
        //            i.ModifiedAtUtc,
        //            i.ModifiedByUserFK
        //        });
        //}

        //[HttpGet, Route("findpaged")]
        //public PageResult<object> FindPaged(ODataQueryOptions<UMAddressDto> options, string searchTerm = null)
        //{
        //    return ApplyPaging(options,umaddressService.Find(searchTerm)
        //        .Select(i => new UMAddressDto()
        //        {
        //            UMAddressK = i.UMAddressK,
        //            StreetAddress1 = i.StreetAddress1,
        //            StreetAddress2 =  i.StreetAddress2,
        //            City = i.City,
        //            State = i.State,
        //            PostalCode = i.PostalCode
        //        }));
        //}
        //#endregion
    }
}
