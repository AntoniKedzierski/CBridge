using Model.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Bidding.Conventions;

public class Convention {

    public required ConventionLevel ConventionLevel { get; set; }

    public required string Decription { get; set; }
}
