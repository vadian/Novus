﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Items {
    public sealed partial class Items : Iitem, Iweapon, Iedible, Icontainer, Iiluminate, Iclothing, Ikey {
        public string DoorID { get; set; }
        public bool SkeletonKey { get; set; }
    }
}
