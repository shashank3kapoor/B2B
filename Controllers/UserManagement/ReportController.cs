using StandAloneB2B.Common.Email;
using StandAloneB2B.Common.FileExport;
using StandAloneB2B.DataAccess.Models;
using StandAloneB2B.Presentation.Models.UserManagement;
using StandAloneB2B.ServiceFramework.Identity;
using StandAloneB2B.ServiceFramework.UserAdministration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Xml;
using System.Xml.Linq;

namespace StandAloneB2B.Presentation.Controllers.UserManagement
{
    [RoutePrefix("api/Reports")]
    public class ReportController : ApiController
    {
        private IUserService userService;
        private IUserCodeService userCodeRepo;
        private IAdministrationSecurityService adminSecurityService;
        private ISystemLogService systemLogService;

        public ReportController(IUserService userService, IUserCodeService userCodeRepo, IAdministrationSecurityService adminSecurityService, ISystemLogService systemLogService)
        {
            this.userService = userService;
            this.userCodeRepo = userCodeRepo;
            this.adminSecurityService = adminSecurityService;
            this.systemLogService = systemLogService;
        }

        [HttpGet, Route("exportuserstocsv")]
        public HttpResponseMessage ExportUsersToCsv()
        {
            var users = userService.Find().Where(i => !i.IsDeleted).ToList();
            var userCsv = new CsvExport();

            foreach (var user in users)
            {
                userCsv.AddRow();
                userCsv["Id"] = user.Id;
                userCsv["FirstName"] = user.FirstName;
                userCsv["LastName"] = user.LastName;
                userCsv["Email"] = user.Email;
                userCsv["LastLoginDate"] = user.LastLoginDateUtc.HasValue ? user.LastLoginDateUtc.Value.ToLocalTime().ToString("MM/dd/yyy hh:mm") : "";
            }

            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(userCsv.ExportToBytes()) };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = "Users_" + DateTime.UtcNow.ToLocalTime().ToString("MMddyyyy_hhmm") + ".csv"
            };
            return result;
        }

        [HttpGet, Route("exportuserstoexcel")]
        public HttpResponseMessage ExportUsersToExcel()
        {
            var users = userService.Find().Where(i => !i.IsDeleted).ToList();
            var userCsv = new CsvExport();

            foreach (var user in users)
            {
                userCsv.AddRow();
                userCsv["Id"] = user.Id;
                userCsv["FirstName"] = user.FirstName;
                userCsv["LastName"] = user.LastName;
                userCsv["Email"] = user.Email;
                userCsv["LastLoginDate"] = user.LastLoginDateUtc.HasValue ? user.LastLoginDateUtc.Value.ToLocalTime().ToString("MM/dd/yyy hh:mm") : "";
            }

            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(userCsv.ExportToBytes()) };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = "Users_" + DateTime.UtcNow.ToLocalTime().ToString("MMddyyyy_hhmm") + ".xls"
            };
            return result;
        }

        [HttpGet, Route("exportuserstoxml")]
        public HttpResponseMessage ExportUsersToXML()
        {
            var users = userService.Find().Where(i => !i.IsDeleted).Select(i => new
            {
                i.Id,
                i.FirstName,
                i.LastName,
                i.UserName,
                i.Email
            }).ToList();

            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    XmlWriterSettings xmlWriterSettings = new XmlWriterSettings()
                    {
                        Indent = true
                    };
                    using (XmlWriter writer = XmlWriter.Create(ms, xmlWriterSettings))
                    {
                        
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml("<Users></Users>");
                        foreach (var user in users)
                        {
                            XmlElement userElem = doc.CreateElement("User");
                            doc.DocumentElement.AppendChild(userElem);
                            
                            XmlElement newElem = doc.CreateElement("FirstName");
                            newElem.InnerText = user.FirstName;
                            userElem.AppendChild(newElem);

                            newElem = doc.CreateElement("LastName");
                            newElem.InnerText = user.LastName;
                            userElem.AppendChild(newElem);

                            newElem = doc.CreateElement("UserName");
                            newElem.InnerText = user.UserName;
                            userElem.AppendChild(newElem);

                            newElem = doc.CreateElement("Email");
                            newElem.InnerText = user.Email;
                            userElem.AppendChild(newElem);
                        }
                        
                        doc.WriteTo(writer); // Write to memorystream
                    }
                    var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(ms.ToArray()) };
                    result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/xml");
                    result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                    {
                        FileName = "Users_" + DateTime.UtcNow.ToLocalTime().ToString("MMddyyyy_hhmm") + ".xml"
                    };
                    return result;
                }
            }
            catch (Exception)
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("Export failed") };
            }
            
            
        }

        [HttpPost, Route("importusers")]
        public IHttpActionResult ImportUsers(dynamic xmlData)
        {
            var importResult = new ImportResultDto();
            var importUsesrDto = new List<ImportUserDto>();
            var savedCount = 0;
            var skippedCount = 0;
            var totalCount = 0;
            var failedCount = 0;
            string xmlDataString = xmlData.xmlData;
            xmlDataString = Regex.Replace(xmlDataString, @"[^\u0020-\u007F]", String.Empty);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlDataString);
            var nodes = doc.ChildNodes;
            foreach (var node in nodes)
            {
                var userNode = node as XmlNode;
                if (userNode.Name == "Users")
                {
                    foreach (var innerNode in userNode.ChildNodes)
                    {
                        var importUserDto = new ImportUserDto();
                        var contentUserNodes = (innerNode as XmlNode).ChildNodes;
                        var firstName = contentUserNodes.Item(0).InnerText;
                        var lastName = contentUserNodes.Item(1).InnerText;
                        var userName = contentUserNodes.Item(2).InnerText;
                        var email = contentUserNodes.Item(3).InnerText;
                        importUserDto.FirstName = firstName;
                        importUserDto.LastName = lastName;
                        importUserDto.UserLogin = userName;
                        importUserDto.Email = email;
                        try
                        {
                            if (userService.IsEmailExist(email))
                            {
                                skippedCount++;
                                totalCount++;
                                importUserDto.Status = "Skipped";
                            }
                            else
                            {
                                if (ModelState.IsValid == false)
                                {
                                    return BadRequest(ModelState);
                                }

                                var newUser = userService.Create(userName, email, firstName,
                                    "", lastName);
                                
                                #region Send one time code for initial change password
                                var userCode = userCodeRepo.Create(new UserCode() { UserK = newUser.Id });
                                string code = userCode.Code;

                                var adminSecurity = adminSecurityService.GetAdministrationSecurity();
                                EmailDto emaildto = new EmailDto()
                                {
                                    EmailBody = String.Format("Hi {0} {1}. You have been added as a new user to siteTRAX Evolution. <br/><br/> Your Onetime code is: <b>{2}</b> <br/> This Onetime code is valid until: <b>{3}</b> at which time it will expire and a new one code will be required to be requested. <br/><br/> To enter your onetime code. Click on \"Forget my password\" then click on \"I have a onetime code\" <br/><br/>If you did not request this password reset, please ignore this message. <br/> Do not reply to this email message as the mail box is un-monitored.", newUser.FirstName, newUser.LastName, userCode.Code, userCode.ExpirationDateUtc.ToLocalTime().ToString("dd-MMMM-yyyy hh:mm tt")),
                                    EmailSubject = "New User - siteTRAX Evolution",
                                    EmailSender = "noreplay.StandAloneB2B.evo@gmail.com",
                                    EmailRecipient = newUser.Email
                                };

                                CustomEmail.SendPasswordEmail(adminSecurity.MailerServer, adminSecurity.MailerServerPort.Value, adminSecurity.MailerUsername, adminSecurity.MailerPassword, adminSecurity.PasswordResetEmail, newUser.Email, emaildto.EmailSubject, emaildto.EmailBody);
                                #endregion
                                savedCount++;
                                importUserDto.Status = "Added";
                            }
                        }
                        catch (Exception)
                        {
                            failedCount++;
                            importUserDto.Status = "Failed";
                        }

                        importUsesrDto.Add(importUserDto);
                    }

                    importResult.ImportUsersDto = importUsesrDto;
                    importResult.Added = savedCount;
                    importResult.Skipped = skippedCount;
                    importResult.Total = totalCount;
                    importResult.Failed = failedCount;
                }
            }

            
            return Ok(importResult);
        }

        [HttpGet, Route("exportuserhistorytocsv")]
        public HttpResponseMessage ExportUserHistoryToCsv(int userK)
        {
            var userHistories = new List<UserHistoryDto>();
            var systemLogs = systemLogService.FindByAffectedUser(null, userK);
            foreach (var systemLog in systemLogs)
            {
                var user = systemLog.UserFK != null ? userService.Read(systemLog.UserFK.Value) : null;
                foreach (var item in systemLog.SystemLogItems)
                {
                    if (item.FieldName != "ModifiedAtUtc" && item.FieldName != "ModifiedByUserFK" && item.FieldName != "CreatedByUserFK" && item.FieldName != "CreatedAtUtc")
                    {
                        var userHistory = new UserHistoryDto();
                        userHistory.DateTime = systemLog.TimestampUtc.ToLocalTime();
                        userHistory.FieldName = item.FieldName;
                        userHistory.PreChangeValue = item.PreChangeValue;
                        userHistory.PostChangeValue = item.PostChangeValue;
                        userHistory.ChangedBy = user != null ? (user.FirstName + " " + user.LastName) : "N/A";
                        userHistories.Add(userHistory);

                    }

                }
            }
            
            var csv = new CsvExport();

            foreach (var userHistory in userHistories)
            {
                csv.AddRow();
                csv["Date Time"] = userHistory.DateTime.ToString("dd-MMM-yyyy hh:mm tt");
                csv["Field Name"] = userHistory.FieldName;
                csv["Pre Change Value"] = userHistory.PreChangeValue;
                csv["Post Change Value"] = userHistory.PostChangeValue;
                csv["Changed By"] = userHistory.ChangedBy;
            }

            var result = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(csv.ExportToBytes()) };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = "User_" + userK + "_Histories_" + DateTime.UtcNow.ToLocalTime().ToString("MMddyyyy_hhmm") + ".csv"
            };
            return result;
        }

    }
}
