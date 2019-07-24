﻿namespace K2Bridge
{
    using System.Collections.Generic;

    internal partial class ElasticSearchDSLVisitor : IVisitor
    {
        public void Visit(Models.Aggregations.DateHistogram dateHistogram)
        {
            // todatetime is redundent but we'll keep it for now
            dateHistogram.KQL = $"{dateHistogram.Metric} by bin(todatetime({dateHistogram.FieldName}), {dateHistogram.Interval})";
        }
    }
}