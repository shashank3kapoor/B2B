using StandAloneB2B.Common.FileUpload;
using StandAloneB2B.DataAccess.Models;
using StandAloneB2B.Presentation.Framework;
using StandAloneB2B.Presentation.Models.UserManagement;
using StandAloneB2B.ServiceFramework.UserAdministration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace StandAloneB2B.Presentation.Controllers.UserManagement
{
    [RoutePrefix("api/UserCertificates")]
    public class UserCertificatesController : SiteTRAXApiController<UserCertificate>
    {
        private IUserCertificateService userCertificateService;

        public UserCertificatesController(IUserCertificateService userCertificateService)
        {
            this.userCertificateService = userCertificateService;
        }

        [HttpPost, Route("addusercertificate")]
        public IHttpActionResult AddUserCertificate(UserCertificate model)
        {
            var userCertificate = userCertificateService.Create(model);
            userCertificateService.SaveChanges();

            return Ok(userCertificate);
        }

        [HttpPost, Route("updateusercertificate")]
        public IHttpActionResult UpdateUserCertificate(UserCertificate model)
        {
            userCertificateService.Update(model.UserCertificateK, model);
            userCertificateService.SaveChanges();

            return Ok();
        }

        [HttpGet, Route("read")]
        public UserCertificateDetailsDto Read(Guid userCertificateK)
        {
            var userCertificate = userCertificateService.Read(userCertificateK);
            var lastHistory = userCertificate.UserCertificateHistories.OrderByDescending(j => j.DateUploaded).FirstOrDefault();
            var detailsDto = new UserCertificateDetailsDto();
            detailsDto.UserCertificate = userCertificate;
            detailsDto.IsApproved = lastHistory == null || !lastHistory.IsApproved ? false : true;

            return detailsDto;
        }

        [HttpDelete, Route("delete/{userCertificateK}")]
        public IHttpActionResult Delete(Guid userCertificateK)
        {
            userCertificateService.SoftDelete(userCertificateK);
            userCertificateService.SaveChanges();

            return Ok();
        }

        [HttpPut, Route("undelete/{userCertificateK}")]
        public IHttpActionResult UnDelete(Guid userCertificateK)
        {
            userCertificateService.UnDelete(userCertificateK);
            userCertificateService.SaveChanges();

            return Ok();
        }

        [HttpGet, Route("getusercertificationhistory")]
        public IQueryable GetUserCertificationHistory(Guid userCertificateK)
        {
            return userCertificateService.FindUserCertificateHistory(userCertificateK).OrderByDescending(i => i.DateUploaded);
        }

        [HttpPost, Route("approveunapprove")]
        public IHttpActionResult ApproveUnapprove(CertificateApproveUnapproveDto model)
        {
            if (!PermissionHelper.HasEffectivePermission(UserK, "Approve/Unapprove certificates", "Business Management Portal"))
                return Ok("You do not have the proper permission to perform this action");


            if (model.BoolVal)
                userCertificateService.ApproveCertificateHistory(model.UserCertificateHistoryK);
            else
                userCertificateService.DisapproveCertificateHistory(model.UserCertificateHistoryK);

            userCertificateService.SaveChanges();

            return Ok();
        }

        

        [HttpPost, Route("uploadcertificatefile")]
        public async Task<HttpResponseMessage> UploadCertificateFile()
        {
            HttpResponseMessage uploadResult;
            string userCertificatesFolder = ConfigurationManager.AppSettings["UserCertificates"];
            uploadResult = await FileUpload.UploadFile(userCertificatesFolder, Request);
            var contentResult = uploadResult.Content.ReadAsStringAsync().Result.Replace("\"", "").Replace(@"\\", @"\").Replace("[", "").Replace("]", "");
            var fileName = Path.GetFileName(contentResult);
            var key = System.Web.HttpUtility.UrlDecode(FileUpload.FormData.GetValues(0).FirstOrDefault());
            key = key.Replace("\"", "");
            if (uploadResult.StatusCode == HttpStatusCode.OK)
                userCertificateService.CreateCertificateHistory(new Guid(key), fileName);

            return uploadResult;
        }
    }

}
