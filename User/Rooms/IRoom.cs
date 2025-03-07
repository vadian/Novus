﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rooms {
    interface IRoom {
        string Id { get; set; }
        string Title { get; set; }
        string Zone {
            get;
        }

        int RoomId {
            get;
        }
       
        string Description { get; set; }
        bool IsDark { get; }
        bool IsLightSourcePresent { get; }
        string Type { get; set; }
        List<Exits> RoomExits { get; }

        Exits GetRoomExit(string direction);
        List<string> GetObjectsInRoom(Room.RoomObjects objectType, double percentage);
        RoomTypes GetRoomType();
        void Save();
    }

    interface IWeather {
        string WeatherMessage { get; set; }
        int WeatherIntensity { get; set; }
    }
}
