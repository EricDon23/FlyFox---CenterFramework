using System;
using UnityEngine;

namespace EnemyBaseLogic
{
    public class Enemy 
    {
        private int health ;
        private int speed ;
        private string  tagName;
        private Vector2 location;

       public int Health
        {
            get { return health; }
            set { health = value; }
        }

        public int Speed
        {
            get { return speed; }
            set { speed = value; }
        }

        public string TagName
        {
            get { return tagName; }
            set { tagName = value; }
        }

        public Vector2 Location
        {
            get { return location; }
            set { location = value; }
        }

        public Enemy(int health, int speed, string tagName, Vector2 location)
        {
            this.health = health;
            this.speed = speed;
            this.tagName = tagName;
            this.location = location;
        }

        public void GetHealth( int health)
        {
           this.health = health;
        }
        
        public void GetSpeed( int speed)
        {
           this.speed = speed;
        }
        public void GetTagName( string tagName)
        {
           this.tagName = tagName;
        }
        public void GetLocation( Vector2 location)
        {
           this.location = location;
        }
        public void GetEnemy()
        {
            Debug.Log("Health: " + health);
            Debug.Log("Speed: " + speed);
            Debug.Log("TagName: " + tagName);
            Debug.Log("Location: " + location);
        }

        
        
    }

}
