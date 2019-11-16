using System;
using System.Collections.Generic;
using System.Text;
using OTHelperNetStandard.Tasks;

namespace OTHelperNetStandard
{
    public static class CachetLogger
    {
        public static void UpdateMetricAndComponent(int componentId, int metricId, TimeSpan diff, string description = null, int secondsBeforePerfProblems = 30, int? overrideStatus = null)
        {
            return;

            //if (TaskRun.IsTestNet)
            //{
            //    return;
            //}

            //try
            //{
            //    var cachet = new Cachet.NET.Cachet("https://status.othub.info/api/v1/", "");

            //    if (cachet.Ping())
            //    {
            //        var component = cachet.GetComponent(componentId);

            //        //1 operational
            //        //2 perf issues
            //        //3 partial outage
            //        //4 major outage
            //        if (overrideStatus.HasValue)
            //        {
            //            component.Status = overrideStatus.Value;
            //        }
            //        else if (diff.TotalSeconds >= secondsBeforePerfProblems)
            //        {
            //            component.Status = 2;
            //        }
            //        else
            //        {
            //            component.Status = 1;
            //        }

            //        if (description != null)
            //        {
            //            component.Description = description;
            //        }

            //        cachet.UpdateComponent(component);

            //        cachet.AddMetricPoint(metricId, (double)diff.TotalMilliseconds, DateTime.UtcNow);
            //    }
            //}
            //catch (Exception ex)
            //{

            //}
        }

        public static void FailComponent(int componentId)
        {
            return;

            //if (TaskRun.IsTestNet)
            //{
            //    return;
            //}

            //try
            //{
            //    var cachet = new Cachet.NET.Cachet("https://status.othub.info/api/v1/", "");

            //    if (cachet.Ping())
            //    {
            //        var component = cachet.GetComponent(componentId);

            //        //1 operational
            //        //2 perf issues
            //        //3 partial outage
            //        //4 major outage
            //        if (component.Status == 1 || component.Status == 2 || component.Status == 0)
            //        {
            //            component.Status = 3;
            //            cachet.UpdateComponent(component);
            //        }
            //        else if (component.Status == 3 || component.Status == 4)
            //        {
            //            DateTime now = DateTime.UtcNow;

            //            var diff = now - component.UpdatedAt;
            //            if (component.Status == 3 && diff.TotalMinutes >= 5)
            //            {
            //                component.Status = 4;
            //                cachet.UpdateComponent(component);
            //            }
            //            else if (component.Status == 4)
            //            {
            //                cachet.UpdateComponent(component);
            //            }
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{

            //}
        }
    }
}