﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Extensions;
using MongoDB.Bson;

namespace AI {
    public class QuestHandler {
        //do I need a list of players and dictionaries?  Wouldn't this NPC just respond to each message in the message queue and 
        //run the script for that person until the script finishes? It would be a bunch of speech and some emotes really.

        private List<string> CurrentQuest { //quest the NPC can give to the player that has not been completed or is in progress
            get;
            set;
        }

        private int QuestStep {  //the current quest a player is assigned to
            get;
            set;
        }

        private BsonDocument Script {
            get;
            set;
        }

        public QuestHandler() { }

        private void GetCurrentQuests(string npcID, string playerID, BsonArray playerQuestsInProgress) {
            //get the list of available quests the NPC can give
            //may need to modify this in the future what if an NPC gets killed?  If they respawn they will have a new ID.
            //maybe some quests qet lost when an NPC dies and maybe some other NPC's can't die only get knocked unconcious. (gonna need a flag for that)
            BsonArray availableQuests = MongoUtils.MongoData.GetCollection("Characters", "NPCQuests").FindOneAs<BsonArray>(MongoDB.Driver.Builders.Query.EQ("_id", npcID));

            //I'm going to want to keep the in-progress and completed quests under the Player information
            //need to find all the in progress quests this player has with this NPC that way they can have multiple quests from the same NPC
            //and not have to visit the NPC once for every single quest he can give.
            foreach (BsonDocument quest in availableQuests) {
                if (playerQuestsInProgress.Contains(quest["ID"])) { //if the player has an in-progress quest that belongs to this NPC load it.
                    CurrentQuest.Add(quest["ID"].AsString);
                }
            }

        }

        public void LoadCurrentQuest(string npcId, string playerId) {




        }

        public void SaveActiveQuests(string npcId) {
            BsonArray activeQuests = new BsonArray();

            BsonDocument quest = new BsonDocument();

            activeQuests.Add(quest);


            BsonDocument result = new BsonDocument();
            result.Add("_id", npcId);
            result.Add("ActiveQuests", activeQuests);

            MongoUtils.MongoData.GetCollection("NPCS", "Quests").Save(result);
        }

        public void AddAQuester(string playerId) {

        }

        private void RemoveAQuester(string playerId) {

        }

        private void IncreaseStep(string playerId) {

        }

        public void PerformScriptstep() {
            //TODO: I need to store the ID of the actual script that is going to be used for this quest unless NPCS are going to have just one script
            //that could possibly be several different quests.  Can't start the next one until you finish the previous one.  That sounds lame and not cool
            //I would rather be able to have the quest the Quester is on and the step for that quest.  Two dictionaries should fix that.
        }
    }
    
}
