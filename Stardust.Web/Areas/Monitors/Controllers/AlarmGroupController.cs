﻿using System;
using System.Collections.Generic;
using NewLife.Cube;
using NewLife.Web;
using Stardust.Data.Monitors;
using XCode.Membership;

namespace Stardust.Web.Areas.Monitors.Controllers
{
    [Menu(20)]
    [MonitorsArea]
    public class AlarmGroupController : EntityController<AlarmGroup>
    {
        static AlarmGroupController()
        {
            LogOnChange = true;

            {
                var df = ListFields.AddListField("History", "CreateUser");
                df.DisplayName = "告警历史";
                df.Header = "告警历史";
                df.Url = "AlarmHistory?groupId={Id}";
            }
            {
                var df = ListFields.AddListField("Log", "CreateUser");
                df.DisplayName = "修改日志";
                df.Header = "修改日志";
                df.Url = "/Admin/Log?category=告警组&linkId={Id}";
            }
        }

        protected override IEnumerable<AlarmGroup> Search(Pager p)
        {
            var id = p["Id"].ToInt(-1);
            if (id > 0)
            {
                var app = AlarmGroup.FindById(id);
                if (app != null) return new[] { app };
            }

            var name = p["name"];
            var enable = p["enable"]?.ToBoolean();

            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();

            return AlarmGroup.Search(name, enable, start, end, p["Q"], p);
        }
    }
}