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
       
        public static void Drop(User.User player, List<string> commands) {
            //1.get the item name from the command, may have to join all the words after dropping the command
            StringBuilder itemName = new StringBuilder();
            Room room = Room.GetRoom(player.Player.Location);

            string full = commands[0];
            commands.RemoveAt(0);
            commands.RemoveAt(0);
            
            foreach (string word in commands) {
                itemName.Append(word + " ");
            }

            int itemPosition = 1;
            string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (position.Count() > 1) {
                int.TryParse(position[position.Count() - 1], out itemPosition);
                itemName = itemName.Remove(itemName.Length - 2, 2);
            }

            //2.get the item from the DB
            List<Items.Iitem> items = Items.Items.GetByName(itemName.ToString().Trim(), player.UserID);
            Items.Iitem item = items[itemPosition - 1];
           
            //3.have player drop item
            string msgPlayer = null;

            if (item != null) {
                player.Player.Inventory.RemoveInventoryItem(item, player.Player.Equipment);
                item.Location = player.Player.Location;
                item.Owner = item.Location.ToString();
                item.Save();

                //4.Inform room and player of action
                string msgOthers = string.Format("{0} drops {1}", player.Player.FirstName, item.Name);
                room.InformPlayersInRoom(msgOthers, new List<string>(new string[] { player.UserID }));
                msgPlayer = string.Format("You drop {0}", item.Name);
            }
            else {
                msgPlayer = "You are not carrying anything of the sorts.";
            }
            
            player.MessageHandler(msgPlayer);
        }

        private static void Loot(User.User player, List<string> commands) {
            Character.Iactor npc = null;
            string[] position = commands[0].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (position.Count() > 1) {
                //ok so the player specified a specific NPC in the room list to loot and not just the first to match
                int pos;
                int.TryParse(position[position.Count() - 1], out pos);
                if (pos != 0) {
                    npc = Character.NPCUtils.GetAnNPCByID(GetObjectInPosition(pos, commands[2], player.Player.Location));
                }
            }
            else {
                npc = Character.NPCUtils.GetAnNPCByID(player.Player.CurrentTarget);
            }

            if (npc != null && npc.IsDead()) {
                npc.Loot(player, commands);
            }
            else if (npc != null && !npc.IsDead()) {
                player.MessageHandler("You can't loot what is not dead! Maybe you should try killing it first.");
            }

            //wasn't an npc we specified so it's probably a player
            if (npc == null) {
                User.User lootee = FindTargetByName(commands[commands.Count - 1], player.Player.Location);
                if (lootee != null && lootee.Player.IsDead()) {
                    lootee.Player.Loot(player, commands);
                }
                else if (lootee != null && !lootee.Player.IsDead()) {
                    player.MessageHandler("You can't loot what is not dead! Maybe you should try pickpocketing or killing it first.");
                }
                else {
                    player.MessageHandler("You can't loot what doesn't exist...unless you see dead people, but you don't.");
                }
            }

            return;
        }


        public static void Unequip(User.User player, List<string> commands) {
            StringBuilder itemName = new StringBuilder();
            int itemPosition = 1;
            string msgPlayer = null;
            string msgOthers = null;

            //they said 'all' so we are going to remove everything
            if (commands.Count > 2 && string.Equals(commands[2].ToLower(), "all", StringComparison.InvariantCultureIgnoreCase)) {
                foreach (KeyValuePair<Items.Wearable, Items.Iitem> item in player.Player.Equipment.GetEquipment()) {
                    if (player.Player.Equipment.UnequipItem(item.Value, player.Player)) {
                    }
                }

                msgOthers = string.Format("{0} removes all his equipment.", player.Player.FirstName);
            }
            else {
                string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
                if (position.Count() > 1) {
                    int.TryParse(position[position.Count() - 1], out itemPosition);
                    itemName = itemName.Remove(itemName.Length - 2, 2);
                }

                string full = commands[0];
                commands.RemoveAt(0);
                commands.RemoveAt(0);

                foreach (string word in commands) {
                    itemName.Append(word + " ");
                }

                List<Items.Iitem> items = Items.Items.GetByName(itemName.ToString().Trim(), player.UserID);
                Items.Iitem item = items[itemPosition - 1];

                if (item != null) {
                    player.Player.Equipment.UnequipItem(item, player.Player);
                    msgOthers = string.Format("{0} unequips {1}", player.Player.FirstName, item.Name);
                    msgPlayer = string.Format("You unequip {0}", item.Name);
                }
                else {
                    if (commands.Count == 2) {
                        msgPlayer = "Unequip what?";
                    }
                    else {
                        msgPlayer = "You don't seem to be equipping that at the moment.";
                    }
                }
            }

            player.MessageHandler(msgPlayer);
            Room.GetRoom(player.Player.Location).InformPlayersInRoom(msgOthers, new List<string>(new string[] { player.UserID }));
        }

        public static void Equip(User.User player, List<string> commands) {
            StringBuilder itemName = new StringBuilder();
            int itemPosition = 1;
            string msgOthers = null;
            string msgPlayer = null;

            //we need to make a list of items to wear from the players inventory and sort them based on stats
            if (commands.Count > 2 && string.Equals(commands[2].ToLower(), "all", StringComparison.InvariantCultureIgnoreCase)) {
                foreach (Items.Iitem item in player.Player.Inventory.GetAllItemsToWear()) {
                    if (player.Player.Equipment.EquipItem(item, player.Player.Inventory)) {
                        msgPlayer += string.Format("You equip {0}.\n", item.Name);
                        msgOthers = string.Format("{0} equips {1}.\n", player.Player.FirstName, item.Name);
                    }
                }
            }
            else {
                string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
                if (position.Count() > 1) {
                    int.TryParse(position[position.Count() - 1], out itemPosition);
                    itemName = itemName.Remove(itemName.Length - 2, 2);
                }

                string full = commands[0];
                commands.RemoveRange(0, 2);

                foreach (string word in commands) {
                    itemName.Append(word + " ");
                }

                List<Items.Iitem> items = Items.Items.GetByName(itemName.ToString().Trim(), player.UserID);
                msgPlayer = null;

                //players need to specify an indexer or we will just give them the first one we found that matched
                Items.Iitem item = items[itemPosition - 1];

                Items.Iweapon weapon = item as Items.Iweapon;

                if (item != null && item.IsWearable) {
                    player.Player.Equipment.EquipItem(item, player.Player.Inventory);
                    if (item.ItemType.ContainsKey(Items.ItemsType.CONTAINER)) {
                        Items.Icontainer container = item as Items.Icontainer;
                        container.Wear();
                    }
                    if (item.ItemType.ContainsKey(Items.ItemsType.CLOTHING)) {
                        Items.Iclothing clothing = item as Items.Iclothing;
                        clothing.Wear();
                    }

                    msgOthers = string.Format("{0} equips {1}.", player.Player.FirstName, item.Name);
                    msgPlayer = string.Format("You equip {0}.", item.Name);
                }
                else if (weapon.IsWieldable) {
                    msgPlayer = "This item can only be wielded not worn.";
                }
                else if (!item.IsWearable || !weapon.IsWieldable) {
                    msgPlayer = "That doesn't seem like something you can wear.";
                }
                else {
                    msgPlayer = "You don't seem to have that in your inventory to be able to wear.";
                }
            }
            
            player.MessageHandler(msgPlayer);
            Room.GetRoom(player.Player.Location).InformPlayersInRoom(msgOthers, new List<string>(new string[] { player.UserID }));
        }

        public static void Wield(User.User player, List<string> commands) {
            StringBuilder itemName = new StringBuilder();
            int itemPosition = 1;
            string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (position.Count() > 1) {
                int.TryParse(position[position.Count() - 1], out itemPosition);
                itemName = itemName.Remove(itemName.Length - 2, 2);
            }

            string full = commands[0];
            commands.RemoveRange(0, 2);
                        
            foreach (string word in commands) {
                itemName.Append(word + " ");
            }
            
            string msgPlayer = null;

            List<Items.Iitem> items = Items.Items.GetByName(itemName.ToString().Trim(), player.UserID);
            Items.Iitem item = items[itemPosition - 1];
            
            Items.Iweapon weapon = (Items.Iweapon)item;
            
            if (weapon != null && weapon.IsWieldable && player.Player.Equipment.GetWieldedWeapons().Count < 2) {
                if (string.IsNullOrEmpty(player.Player.MainHand)) { //no mainhand assigned yet
                    player.Player.MainHand = Items.Wearable.WIELD_RIGHT.ToString(); //we will default to the right hand
                }
                
                player.Player.Equipment.Wield(item, player.Player.Inventory);
                item.Save();
                //TODO: check weapon for any wield perks/curses

                string msgOthers = string.Format("{0} wields {1}", player.Player.FirstName, item.Name);
                Room.GetRoom(player.Player.Location).InformPlayersInRoom(msgOthers, new List<string>(new string[] { player.UserID }));
                msgPlayer = string.Format("You wield {0}", item.Name);
            }
            else if (player.Player.Equipment.GetWieldedWeapons().Count == 2) {
                msgPlayer =  "You are already wielding two weapons...and you don't seem to have a third hand.";
            }
            else if (item.IsWearable) {
                msgPlayer = "This item can only be wielded not worn.";
            }
            else if (!item.IsWearable) {
                msgPlayer = "That not something you can wear or would want to wear.";
            }
            else {
                msgPlayer = "You don't seem to have that in your inventory to be able to wear.";
            }

            player.MessageHandler(msgPlayer);
        }

        public static void Eat(User.User player, List<string> commands) {
            Items.Iitem item = GetItem(commands, player.Player.Location.ToString());
            if (item == null) {
                player.MessageHandler("You don't seem to be carrying that to eat it.");
                return;
            }

            if (item.ItemType.ContainsKey(Items.ItemsType.EDIBLE)) {
               Consume(player, commands, "eat", item);
            }
            else {
                player.MessageHandler("You can't eat that!");
            }
            
        }

        public static void Drink(User.User player, List<string> commands) {
            Items.Iitem item = GetItem(commands, player.Player.Location.ToString());
            if (item == null) {
                player.MessageHandler("You don't seem to be carrying that to drink it.");
                return;
            }
            if (item.ItemType.ContainsKey(Items.ItemsType.DRINKABLE)) {
                Consume(player, commands, "drink", item);
            }
            else {
                player.MessageHandler("You can't drink that!");
            }
            
        }

        private static Items.Iitem GetItem(List<string> commands, string location) {
            StringBuilder itemName = new StringBuilder();
            
            int itemPosition = 1;
            string[] position = commands[commands.Count - 1].Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (position.Count() > 1) {
                int.TryParse(position[position.Count() - 1], out itemPosition);
                itemName = itemName.Remove(itemName.Length - 2, 2);
            }

            string full = commands[0];
            commands.RemoveAt(0);
            commands.RemoveAt(0);
            

            foreach (string word in commands) {
                itemName.Append(word + " ");
            }

            List<Items.Iitem> items = Items.Items.GetByName(itemName.ToString().Trim(), location);
            if (items != null && items.Count > 0) {
                return items[itemPosition - 1];
            }
            else {
                return null;
            }
        }

        private static void Consume(User.User player, List<string> commands, string action, Items.Iitem item){
            string upDown = "gain";
            StringBuilder msgPlayer = new StringBuilder();
            string msgOtherPlayers = null;
                        
            Dictionary<string, double> affectAttributes = null;
            
            Items.Iedible food = item as Items.Iedible;
            affectAttributes = food.Consume();
            
            foreach (KeyValuePair<string, double> attribute in affectAttributes) {
                player.Player.ApplyEffectOnAttribute(attribute.Key.CamelCaseWord(), attribute.Value);
                if (attribute.Value < 0) {
                    upDown = "lost";
                }

                msgPlayer.AppendLine(string.Format("You {0} {1} and {2} {3:F1} points of {4}.", action, item.Name, upDown, Math.Abs(attribute.Value), attribute.Key));
                msgOtherPlayers = string.Format("{0} {1}s {2}", player.Player.FirstName.CamelCaseWord(), action, item.Name);
            }

            //now remove it from the players inventory
            player.Player.Inventory.RemoveInventoryItem(item, player.Player.Equipment);

            //item has been consumed so get rid of it from the DB
            MongoUtils.MongoData.ConnectToDatabase();
            MongoDatabase db = MongoUtils.MongoData.GetDatabase("World");
            MongoCollection col = db.GetCollection("Items");
            col.Remove(Query.EQ("_id", item.Id));

            Room.GetRoom(player.Player.Location).InformPlayersInRoom(msgOtherPlayers, new List<string>(new string[] { player.UserID })); 
            player.MessageHandler(msgPlayer.ToString());
        }

        //container commands
        public static void Put(User.User player, List<string> commands) {
            //this command is used only for putting an Item in the root inventory of a player into a bag.  
            //If an item needs to go from a bag to the root inventory level player should use the GET command instead.

            int itemPosition = 1;
            int containerPosition = 1;
            string itemName = "";
            string containerName = "";

           //this allows players to use either IN or INTO
            int commandIndex = 0;
            foreach (string word in commands) {
                if (string.Equals(word, "in", StringComparison.InvariantCultureIgnoreCase)) {
                    commands[commandIndex] = "into";
                    break;
                }
                commandIndex++;
            }

            string location;
            if (string.Equals(commands[commands.Count - 1], "inventory", StringComparison.InvariantCultureIgnoreCase)) {
                location = null;
                commands.RemoveAt(commands.Count - 1); //get rid of "inventory" se we can parse an index specifier if there is one
            }
            else {
                location = player.Player.Location;
            }

            List<string> commandAltered = ParseItemPositions(commands, "into", out itemPosition, out itemName);
            ParseContainerPosition(commandAltered, "", out containerPosition, out containerName);

            Items.Iitem retrievedItem = null;
            Items.Iitem containerItem = null;

            //using a recursive method we will dig down into each sub container looking for the appropriate container
            if (!string.IsNullOrEmpty(location)) {
                TraverseItems(player, containerName.ToString().Trim(), itemName.ToString().Trim(), containerPosition, itemPosition, out retrievedItem, out containerItem);

                //player is an idiot and probably wanted to put it in his inventory but didn't specify it so let's check there as well
                if (containerItem == null) {
                    foreach (Items.Iitem tempContainer in player.Player.Inventory.GetInventoryAsItemList()) {
                        //Items.Iitem tempContainer = Items.Items.GetByID(id);
                        containerItem = KeepOpening(containerName.CamelCaseString(), tempContainer, containerPosition);
                        if (string.Equals(containerItem.Name, containerName.CamelCaseString(), StringComparison.InvariantCultureIgnoreCase)) {
                            break;
                        }
                    }
                }
            }
            else{ //player specified it is in his inventory 
                foreach (string id in player.Player.Inventory.GetInventoryList()) {
                    Items.Iitem tempContainer = Items.Items.GetByID(id);
                    containerItem = KeepOpening(containerName.CamelCaseString(), tempContainer, containerPosition);
                    if (string.Equals(containerItem.Name, containerName.CamelCaseString(), StringComparison.InvariantCultureIgnoreCase)) {
                        break;
                    }
                }
            }

            bool stored = false;

            retrievedItem = player.Player.Inventory.GetInventoryAsItemList().Where(i => i.Name == itemName).SingleOrDefault();

            if (containerItem != null  && retrievedItem != null) {
                retrievedItem.Location = containerItem.Location;
                retrievedItem.Owner = containerItem.Id.ToString();
                retrievedItem.Save();
                Items.Icontainer container = containerItem as Items.Icontainer;
                stored = container.StoreItem(retrievedItem.Id.ToString());
            }
            

            string msg = null;

            if (!stored) {
                msg = "Could not put " + itemName.ToString().Trim().ToLower() + " inside the " + containerName.ToString().Trim().ToLower() + ".";
            }
            else {
                msg = "You place " + itemName.ToString().Trim().ToLower() + " inside the " + containerName.ToString().Trim().ToLower() + ".";
            }

            player.MessageHandler(msg);
        }

        //TODO: had a bug where I removed item form a container, shut down the game and then both container and player still had the same item (the player even had it duped)
        //needless to say this is bad and fail.
        public static void Get(User.User player, List<string> commands) {
            int itemPosition = 1;
            int containerPosition = 1;
            string itemName = "";
            string containerName = "";

            List<string> commandAltered = ParseItemPositions(commands, "from", out itemPosition, out itemName);
            ParseContainerPosition(commandAltered, commands[3], out containerPosition, out containerName);
          
            string location = player.Player.Location;
           
            Items.Iitem retrievedItem = null;
            Items.Iitem containerItem = null;
            
            //using a recursive method we will dig down into each sub container and look for the appropriate item/container
            TraverseItems(player, containerName.ToString().Trim(), itemName.ToString().Trim(), containerPosition, itemPosition, out retrievedItem, out containerItem);

            string msg = null;
            string msgOthers = null;

            if (retrievedItem != null) {
                Items.Icontainer container = containerItem as Items.Icontainer;
                if (containerItem != null) {
                    retrievedItem = container.RetrieveItem(retrievedItem.Id.ToString());
                    msg = "You take " + retrievedItem.Name.ToLower() + " out of " + containerItem.Name.ToLower() + ".";

                    msgOthers = string.Format("{0} takes {1} out of {2}", player.Player.FirstName, retrievedItem.Name.ToLower(), containerItem.Name.ToLower());
                    
                }
                else {
                    msg = "You get " + retrievedItem.Name.ToLower();
                    msgOthers = string.Format("{0} grabs {1}.", player.Player.FirstName, retrievedItem.Name.ToLower());
                }

                retrievedItem.Location = null;
                retrievedItem.Owner = player.UserID;
                retrievedItem.Save();
                player.Player.Inventory.AddItemToInventory(retrievedItem);
            }
            else {
                msg = "You can't seem to find " + itemName.ToString().Trim().ToLower() + " to grab it.";
            }

            Room.GetRoom(player.Player.Location).InformPlayersInRoom(msgOthers, new List<string>(new string[] { player.UserID }));
            player.MessageHandler(msg);

        }

        private static void ParseContainerPosition(List<string> commands, string separator, out int containerPosition, out string containerName) {
            containerName = "";
            containerPosition = 1;
            StringBuilder containerNameTemp = new StringBuilder();
            bool start = false;

            foreach (string word in commands) {
                if (string.Equals(word, separator, StringComparison.InvariantCultureIgnoreCase)) {
                    start = true;
                    continue;
                }
                if (start) {
                    containerNameTemp.Append(word + " ");
                }
            }

            string[] positionItem = containerNameTemp.ToString().Trim().Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (positionItem.Count() > 1) {
                int.TryParse(positionItem[positionItem.Count() - 1], out containerPosition);
                containerNameTemp.Remove(containerNameTemp.Length - 3, 2);
            }

            containerName = containerNameTemp.ToString().Trim().CamelCaseString();
        }

        private static List<string> ParseItemPositions(List<string> commands, string separator, out int itemPosition, out string itemNameReturned){
            itemPosition = 1;           
            
            string full = commands[0];
            commands.RemoveAt(0);
            commands.RemoveAt(0);

            StringBuilder itemName = GetItemName(commands, separator);
            
            string[] position = itemName.ToString().Trim().Split('.'); //we are separating based on using the decimal operator after the name of the npc/item
            if (position.Count() > 1) {
                int.TryParse(position[position.Count() - 1], out itemPosition);
                itemName.Remove(itemName.Length - 3, 2);
            }

            itemNameReturned = itemName.ToString().Trim().CamelCaseString();
            return commands;
            
        }

        private static void TraverseItems(User.User player, string containerName, string itemName, int containerPosition, int itemPosition, out Items.Iitem retrievedItem, out Items.Iitem retrievedContainer) {
            int containerIndex = 1;
            int itemIndex = 1;
            retrievedItem = null;
            retrievedContainer = null;
            Room room = Room.GetRoom(player.Player.Location);
            if (!string.IsNullOrEmpty(containerName.CamelCaseString())) {
                foreach (string itemID in room.GetObjectsInRoom(Room.RoomObjects.Items)) {
                    Items.Iitem inventoryItem = Items.Items.GetByID(itemID);

                    inventoryItem = KeepOpening(containerName.CamelCaseString(), inventoryItem, containerPosition, containerIndex);

                    if (inventoryItem.Name.Contains(containerName.CamelCaseString())) {
                        retrievedContainer = inventoryItem;
                        break;
                    }
                }
            }           

            //if we retrieved a specific indexed container search within it for the item
            if (retrievedContainer != null) {
                Items.Icontainer container = null;
                container = retrievedContainer as Items.Icontainer;
                foreach (string itemID in container.GetContents()) {
                    Items.Iitem inventoryItem = Items.Items.GetByID(itemID);

                    inventoryItem = KeepOpening(itemName.CamelCaseString(), inventoryItem, itemPosition, itemIndex);

                    if (inventoryItem.Name.Contains(itemName.CamelCaseString())) {
                        retrievedItem = inventoryItem;
                        break;
                    }
                }
            }
            else if (string.IsNullOrEmpty(containerName)) {//we are grabbing a container or an item without a specific index
                foreach (string itemID in room.GetObjectsInRoom(Room.RoomObjects.Items)) {
                    Items.Iitem inventoryItem = Items.Items.GetByID(itemID);

                    inventoryItem = KeepOpening(itemName.CamelCaseString(), inventoryItem, itemPosition, itemIndex);

                    if (inventoryItem.Name.Contains(itemName.CamelCaseString())) {
                        retrievedItem = inventoryItem;
                        break;
                    }
                }
            }
            else {
                foreach (string itemID in room.GetObjectsInRoom(Room.RoomObjects.Items)) {
                    Items.Iitem inventoryItem = Items.Items.GetByID(itemID);
                                     
                    if (inventoryItem.Name.Contains(itemName.CamelCaseString())) {
                        retrievedItem = inventoryItem;
                        break;
                    }
                }
            }
        }      

        private static void Activate(User.User player, List<string> commands) {
            //used for lighting up a lightSource that can be lit.
            Items.Iiluminate lightItem = null;
            string command = null;
            switch (commands[1]) {
                case "TURNON": command = "turn on";
                    break;
                case "SWITHCON": command = "switch on";
                    break;
                default: command = commands[1];
                    break;
            }
            commands.RemoveRange(0, 2);
            List<string> message = new List<string>(); ;
            Room room = Room.GetRoom(player.Player.Location);

            
            lightItem = FindLightInEquipment(commands, player, room);
            

            if (lightItem != null) {

                if (lightItem.isLit == false) {
                    message = lightItem.Ignite();
                    message[1] = ParseMessage(message[1], player, null);
                }
                else {
                    message.Add("It's already on!");
                }
            }
            else {
                message.Add("You don't see anything to " + command + ".");
            }

            player.MessageHandler(message[0]);
            if (message.Count > 1) {
                room.InformPlayersInRoom(message[1], new List<string>(new string[] { player.UserID }));
            }
        }

        private static Items.Iiluminate FindLightInEquipment(List<string> commands, User.User player, Rooms.Room room) {
            Items.Iitem lightItem = null;
            if (commands.Count > 0) {
                string itemName = GetItemName(commands, "").ToString();
                //let's see if player has a lightsource equipped
                foreach (Items.Iitem item in player.Player.Equipment.GetEquipment().Values) {
                    if (item.WornOn == Items.Wearable.WIELD_LEFT || item.WornOn == Items.Wearable.WIELD_RIGHT) {
                        Items.Iiluminate temp = item as Items.Iiluminate;
                        if (temp != null && temp.isLightable) {
                            lightItem = item;
                            break;
                        }
                    }
                }
            }
            else { //let's be smart and figure out what lightSource he wants activated, first come first serve otherwise
                foreach (Items.Iitem item in player.Player.Equipment.GetEquipment().Values) {
                    Items.Iiluminate lightsource = item as Items.Iiluminate;
                    if (lightsource != null && lightsource.isLightable) {
                        lightItem = item;
                        break;
                    }
                }
                if (lightItem == null) { //not in players equipment let's check the room
                    foreach (string itemId in room.GetObjectsInRoom(Room.RoomObjects.Items)) {
                        lightItem = Items.Items.GetByID(itemId);
                        Items.Iiluminate lightsource = lightItem as Items.Iiluminate;
                        if (lightsource != null && lightsource.isLightable) {
                            break;
                        }
                        //if it's a container and it's open see if it has a lightsource inside
                        if (lightItem.ItemType.ContainsKey(Items.ItemsType.CONTAINER)) {
                            Items.Icontainer containerItem = lightItem as Items.Icontainer;
                            if (containerItem.Opened) {
                                foreach (string id in containerItem.GetContents()) {
                                    lightItem = Items.Items.GetByID(itemId);
                                    lightsource = lightItem as Items.Iiluminate;
                                    if (lightsource != null && lightsource.isLightable) {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return (lightItem as Items.Iiluminate);
        }


        private static void DeActivate(User.User player, List<string> commands) {
            //used for turning off a lightSource that can be lit.
            Items.Iiluminate lightItem = null;

            //just making the command be display friendly for the messages
            string command = null;
            switch (commands[1]) {
                case "TURNOFF": command = "turn off";
                    break;
                case "SWITCHOFF": command = "switch off";
                    break;
                default: command = commands[1];
                    break;
            }

            commands.RemoveRange(0, 2);

            List<string> message = new List<string>();
            Room room = Room.GetRoom(player.Player.Location);

            lightItem = FindLightInEquipment(commands, player, room);

            if (lightItem != null) {
                if (lightItem.isLit) {
                    message = lightItem.Extinguish();
                    if (message.Count > 1) {
                        message[1] = string.Format(message[1], player.Player.FirstName);
                    }
                }
                else {
                    message.Add("It's already off!");
                }
            }
            else {
                message.Add("You don't see anything to " + command + ".");
            }

            player.MessageHandler(message[0]);
            if (message.Count > 1) {
                room.InformPlayersInRoom(message[1], new List<string>(new string[] { player.UserID }));
            }
        }


        public static StringBuilder GetItemName(List<string> commands, string separator) {
            StringBuilder itemName = new StringBuilder();
            foreach (string word in commands) {
                if (!string.Equals(word, separator, StringComparison.InvariantCultureIgnoreCase)) {
                    itemName.Append(word + " ");
                }
                else {
                    //we got the item name
                    break;
                }
            }

            return itemName;
        }
    }
}
