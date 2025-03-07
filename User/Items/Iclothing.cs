﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson;

namespace Items {
   public interface Iclothing {
        Wearable EquippedOn { get; set; }
        decimal MaxDefense { get; set; }
        decimal CurrentDefense { get; set; }

        Dictionary<string, double> TargetDefenseEffects { get; set; }
        Dictionary<string, double> PlayerDefenseEffects { get; set; }
        Dictionary<string, double> WearEffects { get; set; }
        void Wear();
    }
}
