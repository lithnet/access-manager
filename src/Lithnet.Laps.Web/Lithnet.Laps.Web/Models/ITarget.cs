using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.Laps.Web.Models
{
    interface ITarget
    {
        TargetType TargetType { get; }
        string TargetName { get; }
    }
}
