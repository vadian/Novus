﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rooms;
using User;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using Extensions;

namespace Commands {
	public partial class CommandParser {
		
		public static void Move(User.User player, List<string> commands) {
            
            if (!player.Player.InCombat) {

                bool foundExit = false;
                string direction = commands[1].ToLower();
                MongoUtils.MongoData.ConnectToDatabase();
                MongoDatabase worldDB = MongoUtils.MongoData.GetDatabase("Commands");
                MongoCollection roomCollection = worldDB.GetCollection("General"); //this is where general command messages are stored

                Room room = Room.GetRoom(player.Player.Location);
                room.GetRoomExits();

                if (direction == "north" || direction == "n") direction = "North";
                else if (direction == "south" || direction == "s") direction = "South";
                else if (direction == "west" || direction == "w") direction = "West";
                else if (direction == "east" || direction == "e") direction = "East";
                else if (direction == "up" || direction == "u") direction = "Up";
                else if (direction == "down" || direction == "d") direction = "Down";
              
                foreach (Exits exit in room.RoomExits) {
                    if (exit.availableExits.ContainsKey(direction.CamelCaseWord())) {

                        //is there a door blocking the exit?
                        bool blocked = false;
                        if (exit.doors.Count > 0 && !exit.doors[exit.Direction.CamelCaseWord()].Open && !exit.doors[exit.Direction.CamelCaseWord()].Destroyed) {
                            blocked = true;
                        }

                        if (!blocked) {
                            player.Player.LastLocation = player.Player.Location;
                            player.Player.Location = exit.availableExits[direction.CamelCaseWord()].Id;
                            player.Player.Save();

                            IMongoQuery query = Query.EQ("_id", "Leaves");
                            BsonDocument leave = roomCollection.FindOneAs<BsonDocument>(query);

                            //semantics
                            if (direction.ToLower() == "up") { 
                                direction = "above"; 
                            }
                            else if (direction.ToLower() == "down") { 
                                direction = "below"; 
                            }


                            string who = player.Player.FirstName;

                            if (room.IsDark) {
                                who = "Someone";
                                direction = "somewhere";
                            }

                            string temp = null;

                            //if the player was just hiding and moves he shows himself
                            if (player.Player.ActionState == CharacterEnums.CharacterActionState.Hiding) {
                                PerformSkill(player, new List<string>(new string[] { "Hide", "Hide" }));
                            }

                            //when sneaking the skill displays the leave/arrive message
                            if (player.Player.ActionState != CharacterEnums.CharacterActionState.Sneaking) {
                                temp = String.Format(leave["ShowOthers"].AsString, who, direction);
                                Room.GetRoom(player.Player.LastLocation).InformPlayersInRoom(temp, new List<string>(new string[] { player.UserID }));
                            }
                            
                            //now we reverse the direction
                            if (direction == "north") direction = "south";
                            else if (direction == "south") direction = "north";
                            else if (direction == "west") direction = "east";
                            else if (direction == "above") direction = "below";
                            else if (direction == "below") direction = "above";
                            else direction = "west";

                            query = Query.EQ("_id", "Arrives");
                            BsonDocument message = roomCollection.FindOneAs<BsonDocument>(query);
                            room = Room.GetRoom(player.Player.Location); //need to get the new room player moved into

                            if (room.IsDark) {
                                who = "Someone";
                                direction = "somewhere";
                            }
                            else {
                                who = player.Player.FirstName;
                            }
                                                                                                                  
                            if (!player.Player.IsNPC) {
                                Look(player, commands);
                            }
                            ApplyRoomModifier(player);

                            if (player.Player.ActionState != CharacterEnums.CharacterActionState.Sneaking) {
                                temp = String.Format(message["ShowOthers"].AsString, who, direction);
                                room.InformPlayersInRoom(temp, new List<string>(new string[] { player.UserID }));
                            }
                 
                            foundExit = true;
                            break;
                        }
                        else {//uh-oh there's a door and it's closed
                            foundExit = true; //we did find an exit it just happens to be blocked by a door
                            if (!player.Player.IsNPC) {
                                string temp = "";
                                IMongoQuery query = Query.EQ("_id", "NoExit");
                                BsonDocument message = roomCollection.FindOneAs<BsonDocument>(query);
                                temp = message["ShowSelf"].AsString;
                                query = Query.EQ("_id", "Blocked");
                                message = roomCollection.FindOneAs<BsonDocument>(query);
                                temp += " " + message["ShowSelf"];
                                query = Query.EQ("_id", "ClosedDoor");
                                message = roomCollection.FindOneAs<BsonDocument>(query);
                                temp = temp.Remove(temp.Length - 1); //get rid of previous line period
                                temp += " because " + String.Format(message["ShowSelf"].AsString.ToLower(), exit.doors[exit.Direction.CamelCaseWord()].Description.ToLower());

                                player.MessageHandler(temp);
                            }
                            break;
                        }
                    }
                    else {
                        continue;
                    }
                }
                if (!foundExit) {
                    IMongoQuery query = Query.EQ("_id", "NoExit");
                    BsonDocument message = roomCollection.FindOneAs<BsonDocument>(query);
                    if (!player.Player.IsNPC) {
                        player.MessageHandler(message["ShowSelf"].AsString + "\r\n");
                    }
                }
            }
            else {
                string msgPlayer = null;
                if (player.Player.InCombat) {
                    msgPlayer = "You can't do that while you are in combat!";
                }
                player.MessageHandler(msgPlayer);
            }
		}

		#region Open things
        public static bool OpenDoor(string roomID, string doorDirection) {
            Door door = FindDoor(roomID, new List<string>() { doorDirection, doorDirection });
            if (door.Openable) {
                if (!door.Open && !door.Locked && !door.Destroyed) {
                    OpenADoor(door);
                    return true;
                }
            }
            return false;
        }

        public static bool OpenDoorOverride(string roomID, string doorDirection) {
            Door door = FindDoor(roomID, new List<string>() { doorDirection, doorDirection });
            if (door.Openable) {
                    OpenADoor(door);
                    return true;
                }
            return false;
        }

		private static void Open(User.User player, List<string> commands) {
			Door door = FindDoor(player.Player.Location, commands);
			if (door != null) {
				OpenDoor(player, door);
                return;
			}

            //ok not a door so then we'll check containers in the room
            OpenContainer(player, commands);
		}

        private static void OpenContainer(User.User player, List<string> commands) {
            //this is a quick work around for knowing which container to open without implementing the dot operator
            //I need to come back and make it work like with NPCS once I've tested everything works correctly
            string location;
            if (string.Equals(commands[commands.Count - 1], "inventory", StringComparison.InvariantCultureIgnoreCase)) {
                location = null;
                commands.RemoveAt(commands.Count - 1); //get rid of "inventory" se we can parse an index specifier if there is one
            }
            else {
                location = player.Player.Location;
            }
            
            string itemNameToGet = Items.Items.ParseItemName(commands);
            bool itemFound = false;

            int itemPosition = 1;
            string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (position.Count() > 1) {
                int.TryParse(position[position.Count() - 1], out itemPosition);
            }
            
            int index = 1;
            Room room = Room.GetRoom(location);
            if (!string.IsNullOrEmpty(location)) {//player didn't specify it was in his inventory check room first
                foreach (string itemID in room.GetObjectsInRoom(Room.RoomObjects.Items)) {
                    Items.Iitem inventoryItem = Items.Items.GetByID(itemID);
                    inventoryItem = KeepOpening(itemNameToGet, inventoryItem, itemPosition);

                    if (string.Equals(inventoryItem.Name, itemNameToGet, StringComparison.InvariantCultureIgnoreCase)) {
                        if (index == itemPosition) {
                            Items.Icontainer container = inventoryItem as Items.Icontainer;
                            player.MessageHandler(container.Open());
                            itemFound = true;
                            break;
                        }
                        else {
                            index++;
                        }
                    }
                }
            }


            if (!itemFound) { //so we didn't find one in the room that matches
                var playerInventory = player.Player.Inventory.GetInventoryAsItemList();
                foreach (Items.Iitem inventoryItem in playerInventory) {
                    if (string.Equals(inventoryItem.Name, itemNameToGet, StringComparison.InvariantCultureIgnoreCase)) {
                        //if player didn't specify an index number loop through all items until we find the want we want otherwise we will
                        // keep going through each item that matches until we hit the index number
                        if (index == itemPosition) {
                            Items.Icontainer container = inventoryItem as Items.Icontainer;
                            player.MessageHandler(container.Open());
                            room.InformPlayersInRoom(player.Player.FirstName + " opens " + inventoryItem.Name.ToLower(), new List<string>(new string[] { player.UserID }));
                            itemFound = true;
                            break;
                        }
                        else {
                            index++;
                        }
                    }
                }
            }

            if (!itemFound) {
                player.MessageHandler("Open what?");
            }
        }

        private static Items.Iitem KeepOpening(string itemName, Items.Iitem item, int itemPosition = 1, int itemIndex = 1) {
            Items.Icontainer container = item as Items.Icontainer;

            if (item.ItemType.ContainsKey(Items.ItemsType.CONTAINER) && container.Contents.Count > 0) {
                foreach (string innerID in container.GetContents()) {
                    Items.Iitem innerItem = Items.Items.GetByID(innerID);
                    if (innerItem != null && KeepOpening(itemName, innerItem, itemPosition, itemIndex).Name.Contains(itemName)) {
                        if (itemIndex == itemPosition) {
                            return innerItem;
                        }
                        else {
                            itemIndex++;
                        }
                    }
                }
            }

            return item;
        }

        private static void OpenADoor(Door door) {
            door.Open = true;
            door.UpdateDoorStatus();
        }

		private static void OpenDoor(User.User player, Door door) {
            if (!player.Player.InCombat) {
                List<string> message = new List<string>();
                Room room = Room.GetRoom(player.Player.Location);
                if (!room.IsDark) {
                    if (door.Openable) {
                        if (!door.Open && !door.Locked && !door.Destroyed) {
                            door.Open = true;
                            door.UpdateDoorStatus();
                            message.Add(String.Format("You open {0} {1}.", GetArticle(door.Description[0]), door.Description));
                            message.Add(String.Format("{0} opens {1} {2}.", player.Player.FirstName, GetArticle(door.Description[0]), door.Description));
                        }
                        else if (door.Open && !door.Destroyed) {
                            message.Add("It's already open.");
                        }
                        else if (door.Locked && !door.Destroyed) {
                            message.Add("You can't open it because it is locked.");
                        }
                        else if (door.Destroyed) {
                            message.Add("It's more than open it's in pieces!");
                        }
                    }
                    else {
                        message.Add("It can't be opened.");
                    }
                }
                else {
                    message.Add("You can't see anything! Let alone what you are trying to open.");
                }

                player.MessageHandler(message[0]);
                if (message.Count > 1) {
                    room.InformPlayersInRoom(message[1], new List<string>(new string[] { player.UserID }));
                }
            }
            else {
                player.MessageHandler("You are in the middle of combat, there are more pressing matters at hand than opening something.");
            }
		}
		#endregion
		
		#region Close things
		private static void Close(User.User player, List<string> commands) {
			List<string> message = new List<string>();

			Door door = FindDoor(player.Player.Location, commands);
			if (door != null) {
				CloseDoor(player, door);
                return;
			}

            CloseContainer(player, commands);
           
		}

        private static void CloseContainer(User.User player, List<string> commands) {
            string location;
            if (string.Equals(commands[commands.Count - 1], "inventory", StringComparison.InvariantCultureIgnoreCase)) {
                location = null;
                commands.RemoveAt(commands.Count - 1); //get rid of "inventory" se we can parse an index specifier if there is one
            }
            else {
                location = player.Player.Location;
            }

            string itemNameToGet = Items.Items.ParseItemName(commands);
            bool itemFound = false;

            Room room = Room.GetRoom(player.Player.Location);

            int itemPosition = 1;
            string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (position.Count() > 1) {
                int.TryParse(position[position.Count() - 1], out itemPosition);
            }
            int index = 1;

            //Here is the real problem, how do I differentiate between a container in the room and one in the players inventory?
            //if a backpack is laying on the ground th eplayer should be able to put stuff in it or take from it, same as if it were
            //in his inventory.  I should probably check room containers first then player inventory otherwise the player can 
            //specify "inventory" to just do it in their inventory container.

            if (!string.IsNullOrEmpty(location)) {//player didn't specify it was in his inventory check room first
                foreach (string itemID in room.GetObjectsInRoom(Room.RoomObjects.Items)) {
                    Items.Iitem inventoryItem = Items.Items.GetByID(itemID);
                    if (string.Equals(inventoryItem.Name, itemNameToGet, StringComparison.InvariantCultureIgnoreCase)) {
                        if (index == itemPosition) {
                            Items.Icontainer container = inventoryItem as Items.Icontainer;
                            player.MessageHandler(container.Close());
                            room.InformPlayersInRoom(player.Player.FirstName + " closes " + inventoryItem.Name.ToLower(), new List<string>(new string[] { player.UserID }));
                            itemFound = true;
                            break;
                        }
                    }
                }
            }


            if (!itemFound) { //so we didn't find one in the room that matches
                var playerInventory = player.Player.Inventory.GetInventoryAsItemList();
                foreach (Items.Iitem inventoryItem in playerInventory) {
                    if (string.Equals(inventoryItem.Name, itemNameToGet, StringComparison.InvariantCultureIgnoreCase)) {
                        //if player didn't specify an index number loop through all items until we find the want we want otherwise we will
                        // keep going through each item that matches until we hit the index number
                        if (index == itemPosition) {
                            Items.Icontainer container = inventoryItem as Items.Icontainer;
                            player.MessageHandler(container.Close());
                            itemFound = true;
                            break;
                        }
                        else {
                            index++;
                        }
                    }
                }
            }
        }

        private static void CloseADoor(Door door) {
            door.Open = false;
            door.UpdateDoorStatus();
        }

        public static bool CloseDoorOverride(string roomID, string doorDirection) {
            Door door = FindDoor(roomID, new List<string>() { doorDirection, doorDirection });
            if (door.Openable) {
                //we only care thats it's open and not destroyed, we bypass any other check
                if (door.Open && !door.Destroyed) {
                    CloseADoor(door);
                    return true;
                }
            }
            return false;
        }

        private static void CloseDoor(User.User player, Door door) {
            if (!player.Player.InCombat) {
                Room room = Room.GetRoom(player.Player.Location);
                List<string> message = new List<string>();
                if (!room.IsDark) {
                    if (door.Openable) {
                        if (door.Open && !door.Destroyed) {
                            door.Open = false;
                            door.UpdateDoorStatus();
                            //I may end up putting these strings in the general collection and then each method just supplies the verb
                            message.Add(String.Format("You close {0} {1}.", GetArticle(door.Description[0]), door.Description));
                            message.Add(String.Format("{0} closes {1} {2}.", player.Player.FirstName, GetArticle(door.Description[0]), door.Description));
                        }
                        else if (door.Destroyed) {
                            message.Add("You can't close it because it is in pieces!");
                        }
                        else {
                            message.Add("It's already closed.");
                        }
                    }
                    else {
                        message.Add("It can't be closed.");
                    }
                }
                else {
                    message.Add("You can't see anything! Let alone what you are trying to close.");
                }

                player.MessageHandler(message[0]);
                if (message.Count > 1) {
                    room.InformPlayersInRoom(message[1], new List<string>(new string[] { player.UserID }));
                }
            }
            else {
                player.MessageHandler("You are in the middle of combat, there are more pressing matters at hand than closing something.");
            }
		}
		#endregion

		#region Lock and Unlock
		private static void Lock(User.User player, List<string> commands) {
			Door door = FindDoor(player.Player.Location, commands);
            if (door != null) {
                LockDoor(player, door);
            }
			//ok not a door so then we'll check containers in the room
		}

        public static bool LockDoorOverride(string roomID, string doorDirection) {
            Door door = FindDoor(roomID, new List<string>() { doorDirection, doorDirection });
            if (!door.Open && !door.Destroyed) {
                door.Locked = true;
                return true;
            }
            return false;
        }

        public static bool UnlockDoorOverride(string roomID, string doorDirection) {
            Door door = FindDoor(roomID, new List<string>() { doorDirection, doorDirection });
            if (!door.Open && !door.Destroyed) {
                door.Locked = false;
                return true;
            }
            return false;
        }

		private static void LockDoor(User.User player, Door door) {
            if (!player.Player.InCombat) {
                List<string> message = new List<string>();
                Room room = Room.GetRoom(player.Player.Location);
                bool hasKey = false;
                if (!room.IsDark) {
                    if (door.Lockable) {
                        if (door.RequiresKey) {
                            //let's see if the player has the key in his inventory or a skeleton key (opens any door)
                            List<Items.Iitem> inventory = player.Player.Inventory.GetInventoryAsItemList();
                            List<Items.Iitem> keyList = inventory.Where(i => i.ItemType.ContainsKey(Items.ItemsType.KEY)).ToList();
                            Items.Ikey key = null;
                            foreach (Items.Iitem keys in keyList) {
                                key = keys as Items.Ikey;
                                if (key.DoorID == door.Id || key.SkeletonKey) {
                                    hasKey = true;
                                    break;
                                }
                            }
                        }
                        if (!door.Open && !door.Destroyed  && ((door.RequiresKey && hasKey) || !door.RequiresKey)) {
                            door.Locked = true;
                            door.UpdateDoorStatus();
                            //I may end up putting these strings in the general collection and then each method just supplies the verb
                            message.Add(String.Format("You lock {0} {1}.", GetArticle(door.Description[0]), door.Description));
                            message.Add(String.Format("{0} locks {1} {2}.", player.Player.FirstName, GetArticle(door.Description[0]), door.Description));
                        }
                        else if (door.Destroyed) {
                            message.Add("Why would you want to lock something that is broken?");
                        }
                        else if (!hasKey) {
                            message.Add("You don't have the key to lock this door.");
                        }
                        else {
                            message.Add("It can't be locked, the door is open.");
                        }
                    }
                    else {
                        message.Add("It can't be locked.");
                    }
                }
                else {
                    message.Add("You can't see anything! Let alone what you are trying to lock.");
                }

                player.MessageHandler(message[0]);
                if (message.Count > 1) {
                    room.InformPlayersInRoom(message[1], new List<string>(new string[] { player.UserID }));
                }
            }
            else {
                player.MessageHandler("You are in the middle of combat there are more pressing matters at hand than locking something.");
            }
		}

		private static void Unlock(User.User player, List<string> commands) {
			List<string> message = new List<string>();

			Door door = FindDoor(player.Player.Location, commands);
			if (door != null) {
				UnlockDoor(player, door);
			}
			//ok not a door so then we'll check containers in the room
		}

		private static void UnlockDoor(User.User player, Door door) {
            if (!player.Player.InCombat) {
                List<string> message = new List<string>();
                Room room = Room.GetRoom(player.Player.Location);
                bool hasKey = false;
                if (!room.IsDark) {
                    if (door.Lockable) {
                        if (door.RequiresKey) {
                            //let's see if the player has the key in his inventory or a skeleton key (opens any door)
                            List<Items.Iitem> inventory = player.Player.Inventory.GetInventoryAsItemList();
                            List<Items.Iitem> keyList = inventory.Where(i => i.ItemType.ContainsKey(Items.ItemsType.KEY)).ToList();
                            Items.Ikey key = null;
                            foreach (Items.Iitem keys in keyList) {
                                key = keys as Items.Ikey;
                                if (key.DoorID == door.Id || key.SkeletonKey) {
                                    hasKey = true;
                                    break;
                                }
                            }
                        }
                        if (!door.Open && !door.Destroyed && ((door.RequiresKey && hasKey) || !door.RequiresKey)) {
                            door.Locked = false;
                            door.UpdateDoorStatus();
                            //I may end up putting these strings in the general collection and then each method just supplies the verb
                            message.Add(String.Format("You unlock {0} {1}.", GetArticle(door.Description[0]), door.Description));
                            message.Add(String.Format("{0} unlocks {1} {2}.", player.Player.FirstName, GetArticle(door.Description[0]), door.Description));
                        }
                        else if (door.Destroyed) {
                            message.Add("Why would you want to unlock something that is in pieces?");
                        }
                        else if (!hasKey) {
                            message.Add("You don't have the key to unlock this door.");
                        }
                        else {
                            message.Add("It can't be unlocked, the door is open.");
                        }
                    }
                    else {
                        message.Add("It can't be unlocked.");
                    }
                }
                else {
                    message.Add("You can't see anything! Let alone what you are trying to unlock.");
                }

                player.MessageHandler(message[0]);
                if (message.Count > 1) {
                    room.InformPlayersInRoom(message[1], new List<string>(new string[] { player.UserID }));
                }
            }
            else {
                player.MessageHandler("You are in the middle of combat there are more pressing matters at hand than unlocking something.");
            }
		}
		#endregion 

		#region Actions    
        private static void PerformSkill(User.User user, List<string> commands) {
            Skill skill = new Skill();
            skill.FillSkill(user, commands);
            skill.ExecuteScript();
        }

       private static void Prone(User.User user, List<string> commands) {
			string message = "";
			if (user.Player.StanceState != CharacterEnums.CharacterStanceState.Prone && (user.Player.ActionState == CharacterEnums.CharacterActionState.None
				|| user.Player.ActionState == CharacterEnums.CharacterActionState.Fighting)) {
					user.Player.SetStanceState(CharacterEnums.CharacterStanceState.Prone);
					user.MessageHandler("You lay down.");
				Room.GetRoom(user.Player.Location).InformPlayersInRoom(String.Format("{0} lays down on the ground.", user.Player.FirstName), new List<string>(new string[]{user.UserID}));
			}
			else if (user.Player.ActionState != CharacterEnums.CharacterActionState.None) {
				message = String.Format("You can't lay prone.  You are {0}!", user.Player.ActionState.ToString().ToLower());
			}
			else {
				message = String.Format("You can't lay prone.  You are {0}!", user.Player.StanceState.ToString().ToLower());
			}
			user.MessageHandler(message);
		}

		private static void Stand(User.User user, List<string> commands) {
			string message = "";
			if (user.Player.StanceState != CharacterEnums.CharacterStanceState.Standing && (user.Player.ActionState == CharacterEnums.CharacterActionState.None
				|| user.Player.ActionState == CharacterEnums.CharacterActionState.Fighting)) {
				user.Player.SetStanceState(CharacterEnums.CharacterStanceState.Standing);
				user.MessageHandler("You stand up.");
                Room.GetRoom(user.Player.Location).InformPlayersInRoom(String.Format("{0} stands up.", user.Player.FirstName), new List<string>(new string[] { user.UserID }));
			}
			else if (user.Player.ActionState != CharacterEnums.CharacterActionState.None) {
				message = String.Format("You can't stand up.  You are {0}!", user.Player.ActionState.ToString().ToLower());
			}
			else {
				message = String.Format("You can't stand up.  You are {0}!", user.Player.StanceState.ToString().ToLower());
			}
			user.MessageHandler(message);
		}

		private static void Sit(User.User user, List<string> commands) {
			string message = "";
			if (user.Player.StanceState != CharacterEnums.CharacterStanceState.Sitting && (user.Player.ActionState == CharacterEnums.CharacterActionState.None
				|| user.Player.ActionState == CharacterEnums.CharacterActionState.Fighting)) {
				user.Player.SetStanceState(CharacterEnums.CharacterStanceState.Sitting);
				user.MessageHandler("You sit down.");
                Room.GetRoom(user.Player.Location).InformPlayersInRoom(String.Format("{0} sits down.", user.Player.FirstName), new List<string>(new string[] { user.UserID }));
			}
			else if (user.Player.ActionState != CharacterEnums.CharacterActionState.None) {
				message = String.Format("You can't sit down.  You are {0}!", user.Player.ActionState.ToString().ToLower());
			}
			else {
				message = String.Format("You can't sit down.  You are {0}!", user.Player.StanceState.ToString().ToLower());
			}
			user.MessageHandler(message);
		}

        
		#endregion Actions

		#region Helper methods
		private static Door FindDoor(string location, List<string> commands) {
			//this needs to be somewhat smart if the player types "break door" we should assume he wants to break the only door
			//in the room, otherwise if he passes in "break iron door" we should be able to figure out he wants to break the door
			//made of iron and if he passes "break west iron door"  he wants to break the iron door in the west exit.
			//Same if he just types "break west door" he wants to break the door in the west exit.
			string[] dirs = new string[] { "north", "south", "east", "west", "up", "down" };
			string objectName = "";
			string possibleDirection = "";
			for (int i = 1; i < commands.Count; i++) {
				if ((i == 1 || i == commands.Count - 1) && dirs.Contains(commands[i])) { //the direction is 99.9% probably going to be at the start or end
					possibleDirection = commands[i];
					continue;
				}
				if (i == 1) continue;  //I don't care about the index which is the action to get the object name
				objectName += commands[i];
				if (i + 1 < commands.Count) objectName += " ";
			}

            Room room = Room.GetRoom(location);
			//let's see if the player provided a direction first
			if (possibleDirection.ToUpper().Contains("NORTH")) possibleDirection = "North";
			else if (possibleDirection.ToUpper().Contains("SOUTH")) possibleDirection = "South";
			else if (possibleDirection.ToUpper().Contains("EAST")) possibleDirection = "East";
			else if (possibleDirection.ToUpper().Contains("WEST")) possibleDirection = "West";
			else if (possibleDirection.ToUpper().Contains("UP") || possibleDirection.ToUpper().Contains("ABOVE")) possibleDirection = "Up";
			else if (possibleDirection.ToUpper().Contains("DOWN") || possibleDirection.ToUpper().Contains("BELOW")) possibleDirection = "Down";

			Door door = null;
			//get the exit based on the direction, if we find an exit then we can start looking for a door
            room.GetRoomExits();
			List<Exits> exits = room.RoomExits;
			Exits exit = exits.Where(e => e.availableExits.ContainsKey(possibleDirection)).SingleOrDefault();

			//let's see if we find one based on a direction
			if (exit != null && !string.IsNullOrEmpty(possibleDirection)) { 
				door = exit.doors[possibleDirection];
			}
			//didn't find anything based on direction, search based on name
			else if (exit == null) { 
				foreach (Exits ex in exits) {
					if (ex.doors.Where(d => d.Value.Name.ToLower().Contains(objectName.ToLower())).Any()) {
						door = ex.doors.Where(d => d.Value.Name.ToLower().Contains(objectName.ToLower())).SingleOrDefault().Value; 
						break; //we found it!
					}
				}
				//we didn't find it, is there even a door in the room at all? let's find the first one we see 
				if (door == null && commands.Count < 2) { //if the player just typed "break" we'll return a door otherwise it could be another object
					foreach (Exits ex in exits) {          //they actually wanted to break.
						if (ex.doors.Count > 0) {
							door = ex.doors.ElementAt(0).Value;
							break; //we found a door
						}
					}
				}
			}
			//at this point we either found one or didn't so we're returning the door we found or null
			return door;
		}


		private static string GetArticle(char firstLetter) {
			string[] vowels = new string[] { "a", "e", "i", "o", "u" };
			string article = vowels.Contains(firstLetter.ToString().ToLower()) ? "an" : "a";
			return article;
		}
		#endregion
	}
}
