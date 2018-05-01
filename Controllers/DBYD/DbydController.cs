using StandAloneB2B.Presentation.Models.Dbyd;
using StandAloneB2B.Presentation.PelicanCorpService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Script.Serialization;

namespace StandAloneB2B.Presentation.Controllers.Dbyd
{
    [Authorize]
    [RoutePrefix("api/pcrp")]
    public class DbydController : ApiController
    {
        [HttpPost, Route("")]
        public string Index(DbydPostModel prms)
        {
            EnquiryProcessClient ep = new EnquiryProcessClient();
            ep.ClientCredentials.UserName.UserName = "testsitetraxau@ucg.com.au";
            ep.ClientCredentials.UserName.Password = "testsitetraxau1";

            var lv_result = "";

            if ( prms.FunName != null )
            {
                switch (prms.FunName)
                {
                    case "HelloWorld":
                        if( prms.HwName != null )
                        {
                            lv_result = ep.HelloWorld(prms.HwName).ToString();
                        }
                        else
                        {
                            lv_result = "HwName is required for Function:" + prms.FunName;
                        }

                        break;

                    case "SubmitEnquiry":
                        SimpleEnquiry se = new SimpleEnquiry();

                        foreach (var lv_se_obj in se.GetType().GetProperties())
                        {
                            string lv_prop_name = lv_se_obj.Name;
                            if (prms.GetType().GetProperty(lv_prop_name) != null)
                            {
                                var lv_prms_val = prms.GetType().GetProperty(lv_prop_name).GetValue(prms, null);
                                se.GetType().GetProperty(lv_prop_name).SetValue(se, lv_prms_val);
                            }
                        }

                        Result[] lv_pCrpResult;
                        lv_pCrpResult = ep.SubmitEnquiry(se);

                        lv_result = new JavaScriptSerializer().Serialize(lv_pCrpResult);
                        break;

                    case "GetWorkingOnBehalfOfList":
                        WorkingOnBehalfOf[] lv_wrkOnBehalfOf = ep.GetWorkingOnBehalfOfList();
                        lv_result = new JavaScriptSerializer().Serialize(lv_wrkOnBehalfOf);
                        break;

                    case "GetWorkingOnBehalfOfAuthorityList":
                        if (prms.WorkingOnBehalfOfId != 0)
                        {
                            WorkingOnBehalfOfAuthority[] lv_wrkOnBehalfOfAuth = ep.GetWorkingOnBehalfOfAuthorityList(prms.WorkingOnBehalfOfId);
                            lv_result = new JavaScriptSerializer().Serialize(lv_wrkOnBehalfOfAuth);
                        }
                        else
                        {
                            lv_result = "'WorkingOnBehalfOfId' is required for Function:" + prms.FunName;
                        }
                        break;

                    case "GetEnquiryPurposeList":
                        EnquiryPurpose[] lv_enqPurpose = ep.GetEnquiryPurposeList();
                        lv_result = new JavaScriptSerializer().Serialize(lv_enqPurpose);
                        break;

                    case "GetWorkplaceLocationList":
                        WorkplaceLocation[] lv_wrkPlcLoc = ep.GetWorkplaceLocationList();
                        lv_result = new JavaScriptSerializer().Serialize(lv_wrkPlcLoc);
                        break;

                    case "CalculateEarliestValidEnquiryStartDateViaPurpose":
                        if (prms.Priority != 0 && prms.EnquiryPurpose != null)
                        {
                            DateTime lv_earlstValidDate = ep.CalculateEarliestValidEnquiryStartDateViaPurpose(prms.Priority, prms.EnquiryPurpose);
                            lv_result = new JavaScriptSerializer().Serialize(lv_earlstValidDate);
                        }
                        else
                        {
                            lv_result = "'Priority & EnquiryPurpose' are required for Function:" + prms.FunName;
                        }
                        break;

                    case "GetPriorityList":
                        lv_result = new JavaScriptSerializer().Serialize(Enum.GetValues(typeof(EnumsPriorityType)));
                        break;

                    case "GetLocationInRoadList":
                        lv_result = new JavaScriptSerializer().Serialize(Enum.GetValues(typeof(EnumsLocationInRoadType)));
                        break;

                    default:
                        lv_result = "Unknown Function: " + prms.FunName;
                        break;
                }
            }
            else
            {
                lv_result = "FunName is required.";
            }

            //Prepare Return Object
            var retObj = new
            {
                result = lv_result
            };

            var lv_json = new JavaScriptSerializer().Serialize(retObj);

            return lv_json;
        }

        [HttpGet, Route("")]
        public string getTest()
        {
            //Prepare Return Object
            var retObj = new
            {
                result = "PCRP-Test"
            };

            var lv_json = new JavaScriptSerializer().Serialize(retObj);

            return lv_json;
        }
    }
}
