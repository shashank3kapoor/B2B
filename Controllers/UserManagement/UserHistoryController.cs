using StandAloneB2B.Presentation.Framework;
using StandAloneB2B.Presentation.Models.UserManagement;
using StandAloneB2B.ServiceFramework.Identity;
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
    [RoutePrefix("api/UserHistories")]
    public class UserHistoryController : SiteTRAXApiController<DataAccess.Models.SystemLog>
    {
        private ISystemLogService systemLogService;
        private IUserService userService;

        public UserHistoryController(ISystemLogService systemLogService, IUserService userService)
        {
            this.systemLogService = systemLogService;
            this.userService = userService;
        }

        [HttpGet, Route("findbyaffecteduser")]
        public PageResult<object> FindByAffectedUser(ODataQueryOptions<UserHistoryDto> options, string searchTerm, int userK)
        {
            var userHistories = new List<UserHistoryDto>();
            var systemLogs = systemLogService.FindByAffectedUser(searchTerm, userK);
            foreach (var systemLog in systemLogs)
            {
                var user = systemLog.UserFK != null ? userService.Read(systemLog.UserFK.Value) : null;
                foreach (var item in systemLog.SystemLogItems)
                {
                    if(item.FieldName != "ModifiedAtUtc" && item.FieldName != "ModifiedByUserFK" && item.FieldName != "CreatedByUserFK" && item.FieldName != "CreatedAtUtc")
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

            return ApplyPaging(options, userHistories.AsQueryable());
        }

    }
}
