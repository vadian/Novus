﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Bson;

namespace MudTime
{

	public class MudTimer 
	{
		private System.Timers.Timer timer;
		public delegate void ElapsedEventHandler(uint ticks, EventArgs e);
		public event ElapsedEventHandler TimerElapsed;
		private uint ticks; //if ticks wraps around we don't want it to be negative
		private int resetAt;

		public MudTimer(int seconds, int reset = 0) { //if zero is passed in as second argument timer will never reset until it truncates
			timer = new System.Timers.Timer(seconds * 1000);
			timer.AutoReset = true;
			timer.Enabled = true;
			timer.Elapsed += new System.Timers.ElapsedEventHandler(BaseTimerElapsed);
			ticks = 1;
			resetAt = reset;
		}

		public void BaseTimerElapsed(object sender, EventArgs e) {
			ticks++;
			if (ticks == resetAt) {
				ticks = 1;
			}
			if (TimerElapsed != null) {
				TimerElapsed(ticks, e);
			}
		}


		//this is where we kick off all the timers for the game
		public static void StartUpTimers() {
			//timer for player driven events that affect stats (regeneration, hunger, poison, etc)
			MudTimer playerTimer = new MudTimer(5, 120); //every 2 minutes
			playerTimer.TimerElapsed += new MudTimer.ElapsedEventHandler(PlayerTimerTick);

            MudTimer timeTimer = new MudTimer(1, 60); 
            timeTimer.TimerElapsed += new MudTimer.ElapsedEventHandler(TimeTimerTick);

            //MudTimer dayTimer = new MudTimer(900, 25); //every 28 seconds in real life equals 1 second game time (12 hours in game = 3 hours real time)
            //dayTimer.TimerElapsed += new MudTimer.ElapsedEventHandler(DayTimerTick);

			MudTimer weatherTimer = new MudTimer(900); //(5400 seconds) every half-hour in real life (2 hours in game) we kick off an attempt at a weather change
			weatherTimer.TimerElapsed += new MudTimer.ElapsedEventHandler(weatherTimerTick);
		}

        private static void TimeTimerTick(uint timeTick, EventArgs e) {
            Calendar.Calendar.UpdateClock(); //1 second in real life is 28 seconds in game time

            Items.Items.DeChargeLightSources();

            if (timeTick % 30 == 0) { //30 seconds
                Rooms.Room.ApplyRoomModifiers((int)timeTick);
            }
        }

	
		private static void PlayerTimerTick(uint playerTicks, EventArgs e) {
            //Every 5 seconds
			Character.NPCUtils.GetInstance().CleanupBonuses(); //NPCs

			foreach (User.User user in MySockets.Server.GetCurrentUserList()) { //Players
                user.Player.CleanupBonuses();
            }

			if (playerTicks % 10 == 0) {
				foreach (User.User user in MySockets.Server.GetCurrentUserList()) {
					foreach (KeyValuePair<string, Character.Attribute> attrib in user.Player.GetAttributes()) {
						user.Player.ApplyRegen(attrib.Key);
					}
				}

				Character.NPCUtils.GetInstance().RegenerateAttributes();
			}

			
		}

		private static void weatherTimerTick(uint ticks, EventArgs e) {
            //pick an area in which to change the weather
                        
            List<string> zones = MongoUtils.MongoData.GetDatabase("Rooms").GetCollectionNames().ToList();
            int numberOfZones = Extensions.RandomNumber.GetRandomNumber().NextNumber(0, zones.Count);

            List<string> zonesToAffect = new List<string>();

            for (int i = 0; i < numberOfZones; i++) {
                string zoneToAdd = zones[Extensions.RandomNumber.GetRandomNumber().NextNumber(0, zones.Count)];
                while (!zonesToAffect.Contains(zoneToAdd)) {
                    zoneToAdd = zones[Extensions.RandomNumber.GetRandomNumber().NextNumber(0, zones.Count)];
                }

                zonesToAffect.Add(zoneToAdd);
            }

            int firstPick = Extensions.RandomNumber.GetRandomNumber().NextNumber(0,5);
            int secondPick = Extensions.RandomNumber.GetRandomNumber().NextNumber(0,5);
			
			//they do not match often at all, I had 1 match within 20+ attempts and I stopped checking at that point
			if (firstPick == secondPick) {
				Calendar.Calendar.ApplyWeather(zonesToAffect);				
			}
		}

		

	}
}
