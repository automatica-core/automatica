﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using Automatica.Core.Model;
using Newtonsoft.Json;

namespace Automatica.Core.EF.Models
{
    public enum RuleInterfaceParameterDataType
    {
        NoParameter = 0,
        Integer = 1,
        Double = 2,
        Text = 3,
        Timer = 4,
        ConstantString = 5,
        Color = 6,
        Enum = 7,
        Bool = 8,
        Calendar = 9,
        Controls = 10
    }

    public class TimerPropertyData : TypedObject
    {
        public DateTime StartTime { get; set; }
        public DateTime StopTime { get; set; }
        public IList<DayOfWeek> EnabledDays { get; set; }
    }

    public class CalendarPropertyData : TypedObject
    {
        public List<CalendarPropertyDataEntry> Value { get; set; }
    }

    public class CalendarPropertyDataEntry : TypedObject
    {
        public bool AllDay { get; set; }
        public string Text { get; set; }
        public string Description { get; set; }
        public bool Disabled { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string RecurrenceRule { get; set; }
        public string RecurrenceException { get; set; }
    }


    public enum RuleInterfaceType
    {
        Unknown = 0,
        Input = 1,
        Status = 2,
        Output = 3
    }

    public class RuleInterfaceTemplate : TypedObject
    {
        public Guid ObjId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ModifiedAt { get; set; }
        public Guid? Owner { get; set; }
        public string Name { get; set; }
        public string Key { get; set; }
        public string Description { get; set; }
        public Guid This2RuleTemplate { get; set; }
        public long This2RuleInterfaceDirection { get; set; }
        public int MaxLinks { get; set; }

        public RuleInterfaceParameterDataType ParameterDataType { get; set; }

        public int SortOrder { get; set; }

        public string DefaultValue { get; set; }

        public bool IsLinkableParameter { get; set; }
        public string Meta { get; set; }
        public string Group { get; set; }

        public RuleInterfaceType InterfaceType { get; set; }

        public RuleInterfaceDirection This2RuleInterfaceDirectionNavigation { get; set; }

        [JsonIgnore]
        public RuleTemplate This2RuleTemplateNavigation { get; set; }
        
    }
}
