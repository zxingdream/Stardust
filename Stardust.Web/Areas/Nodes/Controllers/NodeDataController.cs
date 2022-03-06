﻿using System;
using System.Collections.Generic;
using System.Linq;
using NewLife;
using NewLife.Cube;
using NewLife.Cube.Charts;
using NewLife.Web;
using Stardust.Data.Nodes;
using XCode;
using static Stardust.Data.Nodes.NodeData;

namespace Stardust.Web.Areas.Nodes.Controllers
{
    [Menu(60)]
    [NodesArea]
    public class NodeDataController : ReadOnlyEntityController<NodeData>
    {
        static NodeDataController() => ListFields.RemoveField("Id");

        protected override IEnumerable<NodeData> Search(Pager p)
        {
            PageSetting.EnableAdd = false;

            var nodeId = p["nodeId"].ToInt(-1);

            var start = p["dtStart"].ToDateTime();
            var end = p["dtEnd"].ToDateTime();

            if (nodeId > 0)
            {
                // 最近10小时
                if (p.PageSize == 20 && nodeId > 0) p.PageSize = 24 * 60;

                PageSetting.EnableNavbar = false;
            }

            if (p.Sort.IsNullOrEmpty()) p.OrderBy = _.Id.Desc();

            var list = NodeData.Search(nodeId, start, end, p["Q"], p);

            if (list.Count > 0)
            {
                // 绘制日期曲线图
                var node = Node.FindByID(nodeId);
                if (nodeId >= 0 && node != null)
                {
                    var list2 = list.OrderBy(e => e.Id).ToList();

                    var chart = new ECharts
                    {
                        Title = new ChartTitle { Text = node.Name },
                        Height = 400,
                    };
                    chart.SetX(list2, _.LocalTime, e => e.LocalTime.ToString("HH:mm"));
                    chart.SetY("指标");
                    chart.AddLine(list2, _.CpuRate, e => Math.Round(e.CpuRate * 100), true);

                    var series = chart.Add(list2, _.AvailableMemory, "line", e => node.Memory == 0 ? 0 : (100 - (e.AvailableMemory * 100 / node.Memory)));
                    series.Name = "已用内存";
                    series = chart.Add(list2, _.AvailableFreeSpace, "line", e => node.TotalSize == 0 ? 0 : (100 - (e.AvailableFreeSpace * 100 / node.TotalSize)));
                    series.Name = "已用磁盘";
                    series = chart.Add(list2, _.UplinkSpeed, "line", e => e.UplinkSpeed / 10000);
                    series.Name = "网络上行";
                    series = chart.Add(list2, _.DownlinkSpeed, "line", e => e.DownlinkSpeed / 10000);
                    series.Name = "网络下行";

                    chart.Add(list2, _.TcpConnections);
                    chart.Add(list2, _.TcpTimeWait);
                    chart.Add(list2, _.TcpCloseWait);
                    chart.AddLine(list2, _.Temperature, e => Math.Round(e.Temperature, 2), true);
                    chart.AddLine(list2, _.Battery, e => Math.Round(e.Battery * 100), true);
                    //chart.Add(list2, _.Offset);
                    chart.SetTooltip();
                    ViewBag.Charts = new[] { chart };
                }
            }

            return list;
        }
    }
}