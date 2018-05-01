using Newtonsoft.Json.Linq;
using StandAloneB2B.ServiceFramework.custcodes;
using StandAloneB2B.Common.FileUpload;
using StandAloneB2B.DataAccess;
using StandAloneB2B.DataAccess.Models;
using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace StandAloneB2B.Presentation.Controllers.CustCodes
{
    [RoutePrefix("api/ctrct")]
    public class ContractCodeController : Framework.SiteTRAXApiController<ContractCode, Guid>
    {
        private IContractCodeService contractCodeService;

        public ContractCodeController(IContractCodeService contractCodeService)
        {
            this.contractCodeService = contractCodeService;
        }

        public override IHttpActionResult CreateOrUpdate([FromBody] ContractCode model)
        {
            if (model == null)
            {
                return BadRequest("ContractCode must be provided");
            }

            ContractCode hctrtCode = contractCodeService.Read(model.ContractCodeK);
            if (hctrtCode == null)
            {
                hctrtCode = contractCodeService.Create(model);
                contractCodeService.SaveChanges();
            }
            else
            {
                contractCodeService.Update(hctrtCode.ContractCodeK, model);
            }

            return Ok(hctrtCode);
        }

        [HttpGet, Route("{ctrtCodeK:Guid}")]
        public override SingleResult<ContractCode> Read(Guid ctrtCodeK)
        {
            return SingleResult.Create(contractCodeService.Find(i => i.IsDeleted == false && i.ContractCodeK == ctrtCodeK));
        }

        [HttpPut, Route("{ctrtCodeK:Guid}")]
        public override IHttpActionResult Update(Guid ctrtCodeK, [FromBody] ContractCode model)
        {
            if (model == null)
            {
                return BadRequest("Exchange-Code must be provided");
            }

            ContractCode ctrtCode = contractCodeService.Read(ctrtCodeK);
            ctrtCode.Name = model.Name;

            contractCodeService.SaveChanges();

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
            return contractCodeService.Find(searchTerm);
        }

        [HttpGet, Route("ctrtCode-{ctrtCodeId}")]
        public IQueryable Find(string ctrtCodeId, string searchTerm = null)
        {
            return contractCodeService.Find(searchTerm, i => i.ContractCodeK.ToString().Contains(ctrtCodeId));
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
