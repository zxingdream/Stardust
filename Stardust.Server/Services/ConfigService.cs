﻿using System;
using NewLife;
using NewLife.Threading;
using Stardust.Data.Configs;

namespace Stardust.Server.Services
{
    public class ConfigService
    {
        private TimerX _timer;
        public ConfigService()
        {
            _timer = new TimerX(DoConfigWork, null, 15_000, 60_000) { Async = true };
        }

        /// <summary>为应用解析指定键的值，处理内嵌</summary>
        /// <param name="app"></param>
        /// <param name="cfg"></param>
        /// <param name="scope"></param>
        /// <returns></returns>
        public String Resolve(AppConfig app, ConfigData cfg, String scope)
        {
            var value = cfg?.Value;
            if (value.IsNullOrEmpty()) return value;

            // 要求内嵌全部解析
            var p = 0;
            while (true)
            {
                var p1 = value.IndexOf("${", p);
                if (p1 < 0) break;

                var p2 = value.IndexOf('}', p1 + 2);
                if (p2 < 0) break;

                // 替换
                var item = value.Substring(p1 + 2, p2 - p1 - 2);
                // 拆分 ${key@app:scope}
                var ss = item.Split("@", ":");
                var key2 = ss[0];
                var app2 = ss.Length > 1 ? ss[1] : "";
                var scope2 = ss.Length > 2 ? ss[2] : "";

                //item = replace(key, app, scope) + "";
                {
                    var ap2 = AppConfig.FindByName(app2);
                    var scope3 = !scope2.IsNullOrEmpty() ? scope2 : scope;
                    var cfg2 = ConfigData.Acquire(ap2 ?? app, key2, scope3);
                    if (cfg2 == null) throw new Exception($"在应用[{app}]的[{cfg.Key}]中无法解析[{item}]");

                    item = Resolve(ap2 ?? app, cfg2, scope3);
                }

                // 重新组合
                var left = value.Substring(0, p1);
                var right = value.Substring(p2 + 1);
                value = left + item + right;

                // 移动游标，加速下一次处理
                p = left.Length + item.Length;
            }

            return value;
        }

        private void DoConfigWork(Object state)
        {
            var list = AppConfig.FindAll();
            var next = DateTime.MinValue;
            foreach (var item in list)
            {
                if (!item.Enable || item.PublishTime.Year < 2000) continue;

                // 时间到了，发布，或者计算最近一个到期应用
                if (item.PublishTime <= DateTime.Now)
                    item.Publish();
                else if (item.PublishTime < next || next.Year < 2000)
                    next = item.PublishTime;
            }

            // 如果下一个到期应用时间很短，主动调整定时器
            if (next.Year > 2000)
            {
                var ts = next - DateTime.Now;
                if (ts.TotalMilliseconds < _timer.Period) _timer.SetNext((Int32)ts.TotalMilliseconds);
            }
        }
    }
}