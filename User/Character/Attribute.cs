﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoUtils;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB;

namespace Character {

	public class Attribute : IAttributes {
        protected double _current;
        protected string _name;
        protected double _max;
        protected double _regen;
        protected int _rank;

        //this may also get passed in what type of effect it is to see if the player has something
        //that may lower or increase the number or nullify it completely
        public int Rank {
            get {
                return _rank;
            }
            set {
                _rank = value;
            }
        }
        public double Value {
            get {
                return Math.Round(_current, 0, MidpointRounding.AwayFromZero);
            }
            set {
                _current = value;
            }
        }
        public string Name {
            get {
                return this.ToString();
            }
            set {
                _name = value;
            }
        }
        public double Max {
            get {
                return Math.Round(_max, 0, MidpointRounding.AwayFromZero);
            }
            set {
                _max = value;
            }
        }
        public double RegenRate {
            get {
                return Math.Round(_regen, 2, MidpointRounding.AwayFromZero);
            }
            set {
                _regen = value;
            }
        }

        public virtual void ApplyPositive(double amount) {
            if (amount < 0) {
                amount *= -1;
            }
            _current += amount;
        }

        public virtual void ApplyNegative(double amount) {
            if (amount < 0) {
                amount *= -1;
            }
            _current -= amount;
        }

        public virtual void IncreaseMax(double amount) {
            _max += amount;
        }

        public virtual void DecreaseMax(double amount) {
            _max -= amount;
        }

        public virtual void IncreaseRegen(double amount) {
            _regen += amount;
        }

        public virtual void DecreaseRegen(double amount) {
            _regen -= amount;
        }

        public void IncreaseRank() {
            Rank += 1;
        }

        //this goes off the max health they have and eventually Endurance as well and any other bonuses they may have
        //once the character class starts to get fleshed out much more		
        public virtual bool ApplyRegen() {
            bool applied = false;

            if (_current < _max && _regen > 0) {
                _current += _regen * _max;
                applied = true;
            }

            if (_current > _max) {
                _current = _max;
            }

            return applied;
        }

		public override string ToString() {
			return _name;
		}

		public void ApplyEffect(double amount) {
			if (amount < 0) {
				ApplyNegative(amount);
			}
			else {
				ApplyPositive(amount);
			}
		}       

		public Attribute():this (10, "General Attribute", 10, 0.2, 1) {}

		public Attribute(double amount, string name, double maxAmount, double regenRate, int rank) {
			Value = amount;
			Name = name;
			RegenRate = regenRate;
			Max = maxAmount;
            Rank = rank;
		}
	}
}
