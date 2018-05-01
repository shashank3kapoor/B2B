using Newtonsoft.Json;
using StandAloneB2B.DataAccess.Models;
using StandAloneB2B.Presentation.Models.ucgapi;
using StandAloneB2B.ServiceFramework.custcodes;
using StandAloneB2B.ServiceFramework.ucgapi.chorusapi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace StandAloneB2B.Presentation.Controllers.UCGApi
{
    [Authorize]
    [RoutePrefix("api/ucg")]
    public class UCGApiController : ApiController
    {
        private string unameChorus;
        private string psswdChorus;
        private string apiChorus;

        private string unameBMP;
        private string psswdBMP;
        private string apiBMP;

        private string apiGoogleAddress;
        private string coordsGooglePath;
        private string keyGoogleAddress;

        private int? chorusOrderLimit;
        private string chorusLimitEnable;

        private ITransactionLogService transactionLogService;
        private IEventStageService eventStageService;
        private IEventArchiveService eventArchiveService;
        private IExchangeContractMappingService exchangeContractMappingService;
        public UCGApiController(ITransactionLogService transactionLogService,
                                IEventStageService eventStageService,
                                IEventArchiveService eventArchiveService,
                                IExchangeContractMappingService exchangeContractMappingService)
        {
            this.unameChorus = ConfigurationManager.AppSettings["unameChorus"];
            this.psswdChorus = ConfigurationManager.AppSettings["psswdChorus"];
            this.apiChorus = ConfigurationManager.AppSettings["apiChorus"];

            this.unameBMP = ConfigurationManager.AppSettings["unameBMP"];
            this.psswdBMP = ConfigurationManager.AppSettings["psswdBMP"];
            this.apiBMP = ConfigurationManager.AppSettings["apiBMP"];

            this.apiGoogleAddress = ConfigurationManager.AppSettings["apiGoogleAddress"];
            this.coordsGooglePath = ConfigurationManager.AppSettings["coordsGooglePath"];
            this.keyGoogleAddress = ConfigurationManager.AppSettings["keyGoogleAddress"];

            this.chorusOrderLimit = Convert.ToInt32(ConfigurationManager.AppSettings["chorusOrderLimit"]);
            this.chorusLimitEnable = ConfigurationManager.AppSettings["chorusLimitEnable"];

            this.transactionLogService = transactionLogService;
            this.eventStageService = eventStageService;
            this.eventArchiveService = eventArchiveService;
            this.exchangeContractMappingService = exchangeContractMappingService;
        }

        private HttpClient getGoogleClient()
        {
            try
            {

                HttpClient httpClient = new HttpClient();

                httpClient.DefaultRequestHeaders.Accept.Clear();

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                return httpClient;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet, Route("contractId/{exchange}")]
        public HttpResponseMessage contractId(string exchange)
        {
            try
            {
                if (string.IsNullOrEmpty(exchange) == true)
                {
                    throw new Exception("Exchange Code is required.");
                }
                else
                {
                    IEnumerable qExchangeContractMapping = exchangeContractMappingService.FindContratId(exchange);
                    var response = Request.CreateResponse(HttpStatusCode.OK, qExchangeContractMapping);
                    
                    return response;
                }
                
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        [HttpGet, Route("ggle/{site}")]
        public async Task<HttpResponseMessage> ggle(string site)
        {
            try
            {
                if(string.IsNullOrEmpty(site) == true)
                {
                    throw new Exception("Site is required.");
                }
                else
                {
                    HttpClient httpClient = this.getBearerClient();

                    string gURL = "https://" + this.apiGoogleAddress + this.coordsGooglePath + "?address=" + site + "&key=" + this.keyGoogleAddress;
                    var response = await httpClient.GetAsync(gURL);

                    return response;
                }
                
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        private Dictionary<string, string> GetTokenDetails(string url)
        {
            Dictionary<string, string> tokenDetails = null;
            try
            {
                using (var client = new HttpClient())
                {
                    var login = new Dictionary<string, string>
                   {
                       {"grant_type", "password"},
                       {"username", this.unameBMP},
                       {"password", this.psswdBMP},
                   };

                    var resp = client.PostAsync(url, new FormUrlEncodedContent(login));
                    resp.Wait(TimeSpan.FromSeconds(10));

                    if (resp.IsCompleted)
                    {
                        if (resp.Result.Content.ReadAsStringAsync().Result.Contains("access_token"))
                        {
                            tokenDetails = JsonConvert.DeserializeObject<Dictionary<string, string>>(resp.Result.Content.ReadAsStringAsync().Result);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return tokenDetails;
        }

        private HttpClient getBearerClient()
        {
            try
            {

                HttpClient httpClient = new HttpClient();
                var tokenDetails = this.GetTokenDetails("http://" + this.apiBMP + "/token");
                string bearer = null;

                foreach (var rec in tokenDetails)
                {
                    if (rec.Key == "access_token")
                    {
                        bearer = rec.Value;
                        break;
                    }
                }

                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                //httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", string.Format("{0} {1}", "Bearer", bearer));

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                return httpClient;
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        private HttpClient getBasicClient()
        {
            try
            {
                var handler = new HttpClientHandler { AllowAutoRedirect = false };
                HttpClient httpClient = new HttpClient(handler);

                var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", this.unameChorus, this.psswdChorus)));

                httpClient.DefaultRequestHeaders.Clear();

                httpClient.BaseAddress = new Uri("https://" + this.apiChorus + "/");
                httpClient.DefaultRequestHeaders.Host = this.apiChorus;
                
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
                //httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-www-form-urlencoded"));


                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

                return httpClient;
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        [HttpGet, Route("bmpmenu")]
        public async Task<HttpResponseMessage> BMPMenu()
        {
            HttpClient httpClient = this.getBearerClient();

            var response = await httpClient.GetAsync("http://" + this.apiBMP + "/api/BusinessManagement/Menus/_all/");

            return response;
        }

        [HttpGet, Route("order/{id}")]
        public async Task<HttpResponseMessage> Order(string id)
        {
            try
            {

                HttpClient httpClient = this.getBasicClient();

                TransactionLog tLog = transactionLogService.Create(new TransactionLog());
                tLog.TransactionLogK = Guid.NewGuid();
                string unqTID = tLog.TransactionLogK.ToString();
                httpClient.DefaultRequestHeaders.Add("X-Transaction-ID", unqTID);

                string apiUri = "https://" + this.apiChorus + "/orders/" + id;
                var response = await httpClient.GetAsync(new Uri(apiUri));

                tLog.Authorization = httpClient.DefaultRequestHeaders.Authorization.ToString();
                tLog.BaseAddress = apiUri;
                tLog.Response = response.ToString();
                tLog.ResponseBody = await response.Content.ReadAsStringAsync();
                transactionLogService.SaveChanges();

                return response;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet, Route("order/{id}/memo")]
        public async Task<HttpResponseMessage> Memo(string id)
        {
            try
            { 
                HttpClient httpClient = this.getBasicClient();

                TransactionLog tLog = transactionLogService.Create(new TransactionLog());
                tLog.TransactionLogK = Guid.NewGuid();
                string unqTID = tLog.TransactionLogK.ToString();
                httpClient.DefaultRequestHeaders.Add("X-Transaction-ID", unqTID);

                string apiUri = "https://" + this.apiChorus + "/orders/" + id + "/memo";
                var response = await httpClient.GetAsync(new Uri(apiUri));

                tLog.Authorization = httpClient.DefaultRequestHeaders.Authorization.ToString();
                tLog.BaseAddress = apiUri;
                tLog.Response = response.ToString();
                tLog.ResponseBody = await response.Content.ReadAsStringAsync();
                transactionLogService.SaveChanges();

                return response;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet, Route("order/{id}/instructions")]
        public async Task<HttpResponseMessage> Instructions(string id)
        {
            try
            { 
                HttpClient httpClient = this.getBasicClient();

                TransactionLog tLog = transactionLogService.Create(new TransactionLog());
                tLog.TransactionLogK = Guid.NewGuid();
                string unqTID = tLog.TransactionLogK.ToString();
                httpClient.DefaultRequestHeaders.Add("X-Transaction-ID", unqTID);

                string apiUri = "https://" + this.apiChorus + "/orders/" + id + "/instructions";
                var response = await httpClient.GetAsync(new Uri(apiUri));

                tLog.Authorization = httpClient.DefaultRequestHeaders.Authorization.ToString();
                tLog.BaseAddress = apiUri;
                tLog.Response = response.ToString();
                tLog.ResponseBody = await response.Content.ReadAsStringAsync();
                transactionLogService.SaveChanges();

                return response;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpGet, Route("events/{interval}")]
        public async Task<HttpResponseMessage> Events(string interval)
        {
            try
            { 
                HttpClient httpClient = this.getBasicClient();

                TransactionLog tLog = transactionLogService.Create(new TransactionLog());
                tLog.TransactionLogK = Guid.NewGuid();
                string unqTID = tLog.TransactionLogK.ToString();
                httpClient.DefaultRequestHeaders.Add("X-Transaction-ID", unqTID);

                string apiUri = "https://" + this.apiChorus + "/events?interval=" + interval;
                var response = await httpClient.GetAsync(new Uri(apiUri));

                tLog.Authorization = httpClient.DefaultRequestHeaders.Authorization.ToString();
                tLog.BaseAddress = apiUri;
                tLog.Response = response.ToString();
                tLog.ResponseBody = await response.Content.ReadAsStringAsync();
                transactionLogService.SaveChanges();

                return response;
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        [HttpGet, Route("getEvents/{interval}")]
        public async Task<HttpResponseMessage> GetEvents(string interval)
        {
            try
            {
                HttpClient httpClient = this.getBasicClient();

                TransactionLog tLog = transactionLogService.Create(new TransactionLog());
                tLog.TransactionLogK = Guid.NewGuid();
                string unqTID = tLog.TransactionLogK.ToString();
                httpClient.DefaultRequestHeaders.Add("X-Transaction-ID", unqTID);

                string apiUri = "https://" + this.apiChorus + "/events?interval=" + interval;
                var response = await httpClient.GetAsync(new Uri(apiUri));

                tLog.Authorization = httpClient.DefaultRequestHeaders.Authorization.ToString();
                tLog.BaseAddress = apiUri;
                tLog.Response = response.ToString();
                tLog.ResponseBody = await response.Content.ReadAsStringAsync();
                transactionLogService.SaveChanges();

                if (response.IsSuccessStatusCode)
                {
                    JavaScriptSerializer oJS = new JavaScriptSerializer();
                    List<EventStage> listEventStage = new List<EventStage>();
                    listEventStage = oJS.Deserialize<List<EventStage>>(tLog.ResponseBody);

                    //Existing data in EventStage
                    IQueryable<EventStage> esExtgData = eventStageService.Find();

                    //Existing data in EventArchive
                    IQueryable<EventArchive> eaExtgData = eventArchiveService.Find();

                    //New inserts in EventStage
                    List<EventStage> esNewData = new List<EventStage>();

                    //New inserts in EventArchive
                    List<EventArchive> eaNewData = new List<EventArchive>();

                    if (this.chorusLimitEnable == "true")
                    {
                        int totalCount = esExtgData.Count();
                        if (totalCount >= this.chorusOrderLimit)
                        {
                            throw new HttpResponseException(
                                Request.CreateErrorResponse(HttpStatusCode.NotFound,
                                    "Limit of " + this.chorusOrderLimit + " Service-Orders reached.")
                            );
                        }
                    }

                    //Insert data from payload
                    foreach (EventStage esItm in listEventStage)
                    {
                        if( this.chorusLimitEnable == "true" && esNewData != null)
                        {
                            int totalCount = esExtgData.Count() + esNewData.Count();
                            if( totalCount >= this.chorusOrderLimit )
                            {
                                break;
                            }
                        }
                        EventStage esFound = (from es in esExtgData
                                             where es.OrderID == esItm.OrderID && es.ID == esItm.ID
                                             select es).FirstOrDefault();
                        EventStage esNewFound = null;
                        if (esNewData != null)
                        {
                            esNewFound = esNewData.Where(es => es.OrderID == esItm.OrderID 
                                                && es.ID == esItm.ID).FirstOrDefault();
                        }

                        if (esFound == null && esNewFound == null)
                        {
                            eventStageService.Create(esItm);
                            esNewData.Add(esItm);
                        }

                        EventArchive eaFound = (from ea in eaExtgData
                                                where ea.OrderID == esItm.OrderID
                                                  && ea.SOStage == esItm.SOStage
                                                  && ea.SOType == esItm.SOType
                                                  && ea.ASID == esItm.ASID
                                                  && ea.ID == esItm.ID
                                                select ea).FirstOrDefault();

                        EventArchive eaNewFound = null;
                        if (eaNewData != null) {
                            eaNewFound = eaNewData.Where(ea => ea.OrderID == esItm.OrderID
                                                     && ea.SOStage == esItm.SOStage
                                                     && ea.SOType == esItm.SOType
                                                     && ea.ASID == esItm.ASID
                                                     && ea.ID == esItm.ID).FirstOrDefault();
                        }

                        if (eaFound == null && eaNewFound == null)
                        {
                            EventArchive eaTmp = new EventArchive();
                            eaTmp.ID = esItm.ID;
                            eaTmp.EventAction = esItm.EventAction;
                            eaTmp.OrderID = esItm.OrderID;
                            eaTmp.SOType = esItm.SOType;
                            eaTmp.SOStage = esItm.SOStage;
                            eaTmp.CrtDate = esItm.CrtDate;
                            eaTmp.ASID = esItm.ASID;

                            eventArchiveService.Create(eaTmp);
                            eaNewData.Add(eaTmp);
                        }
                        
                    }

                    //Save and check if EventStage-table-modified while execution
                    bool saveFailed;
                    do
                    {
                        saveFailed = false;
                        try
                        {
                            eventStageService.SaveChanges();
                        }
                        catch (DbUpdateConcurrencyException ex)
                        {
                            saveFailed = true;

                            // Update original values from the database 
                            var entry = ex.Entries.Single();
                            entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                        }

                    } while (saveFailed);

                    //Save and check if EventArchive-table-modified while execution
                    do
                    {
                        saveFailed = false;
                        try
                        {
                            eventArchiveService.SaveChanges();
                        }
                        catch (DbUpdateConcurrencyException ex)
                        {
                            saveFailed = true;

                            // Update original values from the database 
                            var entry = ex.Entries.Single();
                            entry.OriginalValues.SetValues(entry.GetDatabaseValues());
                        }

                    } while (saveFailed);

                    response = Request.CreateResponse(HttpStatusCode.OK, eaNewData);
                }

                return response;
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        [HttpPut, Route("order/{id}/stage")]
        public async Task<HttpResponseMessage> Stage(string id, [FromBody] SalesOrderDto data)
        {
            if (!ModelState.IsValid)
            {
                //return ActionContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, ModelState.Values.SelectMany(e => e.Errors.Select(er => er.ErrorMessage)).ToString());
                return ActionContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, ActionContext.ModelState);
            }

            try
            {
                HttpClient httpClient = this.getBasicClient();

                TransactionLog tLog = transactionLogService.Create(new TransactionLog());
                tLog.TransactionLogK = Guid.NewGuid();
                string unqTID = tLog.TransactionLogK.ToString();
                httpClient.DefaultRequestHeaders.Add("X-Transaction-ID", unqTID);

                string apiUri = "https://" + this.apiChorus + "/orders/" + id + "/stage";
                string jsData = JsonConvert.SerializeObject(data);
                var response = await httpClient.PutAsync(new Uri(apiUri), new StringContent(jsData, Encoding.UTF8, "application/json"));

                tLog.Authorization = httpClient.DefaultRequestHeaders.Authorization.ToString();
                tLog.BaseAddress = apiUri;
                tLog.Response = response.ToString();
                tLog.ResponseBody = await response.Content.ReadAsStringAsync();
                transactionLogService.SaveChanges();

                return response;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpPut, Route("order/{id}/memo")]
        public async Task<HttpResponseMessage> Memo(string id, [FromBody] SOMemoDto data)
        {
            if (!ModelState.IsValid)
            {
                return ActionContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, ActionContext.ModelState);
            }

            try
            {

                HttpClient httpClient = this.getBasicClient();

                TransactionLog tLog = transactionLogService.Create(new TransactionLog());
                tLog.TransactionLogK = Guid.NewGuid();
                string unqTID = tLog.TransactionLogK.ToString();
                httpClient.DefaultRequestHeaders.Add("X-Transaction-ID", unqTID);

                string place = "bottom";
                if(String.IsNullOrEmpty(data.place) != true)
                {
                    place = data.place;
                }
                string apiUri = "https://" + this.apiChorus + "/orders/" + id + "/memo?place=" + place;

                string jsData = JsonConvert.SerializeObject(data);
                var response = await httpClient.PutAsync(new Uri(apiUri), new StringContent(jsData, Encoding.UTF8, "application/json"));

                tLog.Authorization = httpClient.DefaultRequestHeaders.Authorization.ToString();
                tLog.BaseAddress = apiUri;
                tLog.Response = response.ToString();
                tLog.ResponseBody = await response.Content.ReadAsStringAsync();
                transactionLogService.SaveChanges();

                return response;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        [HttpPut, Route("order/{id}/duedate")]
        public async Task<HttpResponseMessage> DueDate(string id, [FromBody] SODueDateDto data)
        {
            if (!ModelState.IsValid)
            {
                return ActionContext.Request.CreateErrorResponse(HttpStatusCode.BadRequest, ActionContext.ModelState);
            }

            try
            {
                HttpClient httpClient = this.getBasicClient();

                TransactionLog tLog = transactionLogService.Create(new TransactionLog());
                tLog.TransactionLogK = Guid.NewGuid();
                string unqTID = tLog.TransactionLogK.ToString();
                httpClient.DefaultRequestHeaders.Add("X-Transaction-ID", unqTID);

                string apiUri = "https://" + this.apiChorus + "/orders/" + id + "/duedate";
                string jsData = JsonConvert.SerializeObject(data);
                var response = await httpClient.PutAsync(new Uri(apiUri), new StringContent(jsData, Encoding.UTF8, "application/json"));

                tLog.Authorization = httpClient.DefaultRequestHeaders.Authorization.ToString();
                tLog.BaseAddress = apiUri;
                tLog.Response = response.ToString();
                tLog.ResponseBody = await response.Content.ReadAsStringAsync();
                transactionLogService.SaveChanges();

                return response;
            }
            catch (Exception e)
            {
                throw e;
            }
        }
    }
}
