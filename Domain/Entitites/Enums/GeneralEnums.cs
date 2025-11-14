using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entitites.Enums
{
    public enum MessageRole
    {
        System,
        User,
    }
    
    public enum LoginMethod
    {
        Email = 0,
        Google = 1
    }

    public enum SubscriptionPlan
    {
        OneMonth = 1,
        ThreeMonths = 3,
        SixMonths = 6,
        OneYear = 12
    }

    public enum CompanyStatus
    {
        Active,
        Paused
    }

    public enum AI_ConfigureKind
    {
        Global,
        Company
    }
}
