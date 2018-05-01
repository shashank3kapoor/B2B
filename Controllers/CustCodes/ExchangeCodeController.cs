using Newtonsoft.Json.Linq;
using StandAloneB2B.ServiceFramework.custcodes;
using StandAloneB2B.Services.custcodes;
using StandAloneB2B.Common.FileUpload;
using StandAloneB2B.DataAccess;
using StandAloneB2B.DataAccess.Models;
using StandAloneB2B.Presentation.Framework;
using System;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace StandAloneB2B.Presentation.Controllers.CustCodes
{
    [RoutePrefix("api/xchg")]
    public class ExchangeCodeController : SiteTRAXApiController<ExchangeCode, Guid>
    {
        private IExchangeCodeService exchangeCodeService;

        public ExchangeCodeController(IExchangeCodeService exchangeCodeService)
        {
            this.exchangeCodeService = exchangeCodeService;
        }

        public override IHttpActionResult CreateOrUpdate([FromBody] ExchangeCode model)
        {
            if (model == null)
            {
                return BadRequest("ExchangeCode must be provided");
            }

            ExchangeCode hexhgCode = exchangeCodeService.Read(model.ExchangeCodeK);
            if (hexhgCode == null)
            {
                hexhgCode = exchangeCodeService.Create(model);
                exchangeCodeService.SaveChanges();
            }
            else
            {
                exchangeCodeService.Update(hexhgCode.ExchangeCodeK, model);
            }

            return Ok(hexhgCode);
        }

        [HttpGet, Route("{exhgCodeK:Guid}")]
        public override SingleResult<ExchangeCode> Read(Guid exhgCodeK)
        {
            return SingleResult.Create(exchangeCodeService.Find(i => i.IsDeleted == false && i.ExchangeCodeK == exhgCodeK));
        }

        [HttpPut, Route("{exhgCodeK:Guid}")]
        public override IHttpActionResult Update(Guid exhgCodeK, [FromBody] ExchangeCode model)
        {
            if (model == null)
            {
                return BadRequest("Exchange-Code must be provided");
            }

            ExchangeCode exhgCode = exchangeCodeService.Read(exhgCodeK);
            exhgCode.Name = model.Name;

            exchangeCodeService.SaveChanges();

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
            return exchangeCodeService.Find(searchTerm);
        }

        [HttpGet, Route("exhgCode-{exhgCodeId}")]
        public IQueryable Find(string exhgCodeId, string searchTerm = null)
        {
            return exchangeCodeService.Find(searchTerm, i => i.ExchangeCodeK.ToString().Contains(exhgCodeId));
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
