﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace Items {
    public interface Iedible {
        void GetAttributesAffected(BsonArray attributesToAffect);
        Dictionary<string, double> Consume();
    }
}
