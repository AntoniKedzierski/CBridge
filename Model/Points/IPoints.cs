using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Points;

public interface IPoints {

    public int CalculatePoints(bool noTrumpGame = false);
}
