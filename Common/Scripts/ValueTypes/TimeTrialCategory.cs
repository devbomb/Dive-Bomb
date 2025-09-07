using System;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Godot;

namespace FastDragon
{
    public enum TimeTrialCategory
    {
        [Display(Name = "Any %")]
        AnyPercent,

        [Display(Name = "All Fairies")]
        FairyPercent
    }

    public static class TimeTrialCategoryExtensions
    {
        private static readonly ConcurrentDictionary<TimeTrialCategory, string> _nameCache = new();
        public static string HumanReadableName(this TimeTrialCategory category)
        {
            return _nameCache.GetOrAdd(category, category =>
            {
                return category.GetType()
                        .GetMember(category.ToString())
                        .First()
                        .GetCustomAttribute<DisplayAttribute>()
                        .Name;
            });
        }
    }
}