using Microsoft.IdentityModel.Protocols;
using Newtonsoft.Json.Linq;
using StandAloneB2B.Common.FileUpload;
using StandAloneB2B.DataAccess;
using StandAloneB2B.DataAccess.Models;
using StandAloneB2B.Presentation.Framework;
using StandAloneB2B.ServiceFramework.custcodes;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace StandAloneB2B.Presentation.Controllers.CustCodes
{
    [RoutePrefix("api/xhgCrctMpg")]
    public class ExchangeContractMappingController : SiteTRAXApiController<ExchangeContractMapping, Guid>
    {
        private IExchangeContractMappingService exchangeContractMappingService;

        public ExchangeContractMappingController(IExchangeContractMappingService exchangeContractMappingService)
        {
            this.exchangeContractMappingService = exchangeContractMappingService;
        }

        public override IHttpActionResult CreateOrUpdate([FromBody] ExchangeContractMapping model)
        {
            if (model == null)
            {
                return BadRequest("ExchangeContractMapping must be provided");
            }

            ExchangeContractMapping xchgCtrctMapping = exchangeContractMappingService.Read(model.ExchangeContractMappingK);
            if (xchgCtrctMapping == null)
            {
                xchgCtrctMapping = exchangeContractMappingService.Create(model);
                exchangeContractMappingService.SaveChanges();
            }
            else
            {
                exchangeContractMappingService.Update(xchgCtrctMapping.ExchangeContractMappingK, model);
            }

            return Ok(xchgCtrctMapping);
        }

        [HttpGet, Route("{xchgCtrctMappingK:Guid}")]
        public override SingleResult<ExchangeContractMapping> Read(Guid xchgCtrctMappingK)
        {
            return SingleResult.Create(exchangeContractMappingService.Find(i => i.IsDeleted == false && i.ExchangeContractMappingK == xchgCtrctMappingK));
        }

        [HttpPut, Route("{xchgCtrctMappingK:Guid}")]
        public override IHttpActionResult Update(Guid xchgCtrctMappingK, [FromBody] ExchangeContractMapping model)
        {
            if (model == null)
            {
                return BadRequest("Exchange-Contract-Mapping must be provided");
            }

            ExchangeContractMapping xchgCtrctMapping = exchangeContractMappingService.Read(xchgCtrctMappingK);
            xchgCtrctMapping.ExchangeCodeFK = model.ExchangeCodeFK;
            xchgCtrctMapping.ContractCodeFK = model.ContractCodeFK;

            exchangeContractMappingService.SaveChanges();

            return Ok();
        }

        [HttpPatch]
        public override IHttpActionResult Patch(Guid id, [FromBody]JObject model)
        {
            using (CoreDB db = new CoreDB(UserK))
            {
                db.SaveChanges();
            }

            return Ok();
        }

        public override IQueryable Find(string searchTerm = null)
        {
            return exchangeContractMappingService.Find(searchTerm);
        }

        [HttpGet, Route("xchgCtrctMapping-{xchgCtrctMappingId}")]
        public IQueryable Find(string xchgCtrctMappingId, string searchTerm = null)
        {
            return exchangeContractMappingService.Find(searchTerm, i => i.ExchangeContractMappingK.ToString().Contains(xchgCtrctMappingId));
        }


        [System.Web.Http.HttpPost, System.Web.Http.Route("uploadFile")]
        public async Task<HttpResponseMessage> UploadData()
        {
            HttpResponseMessage uploadResult;
            string artefactSampleFolder = ConfigurationManager.AppSettings["ArtefactSampleFolder"];
            uploadResult = await FileUpload.UploadFile(artefactSampleFolder, Request);
            return uploadResult;
        }

        [System.Web.Http.HttpGet, System.Web.Http.Route("FolderPath")]
        public string getFolderPath()
        {
            return FileUpload.GetPath("ArtefactSampleFolder");
        }
    }
}
