using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace CSVParser.Example
{
    [Serializable]
    public class AnimalInfoSimple
    {
        [SerializeField] private int ID;
        [SerializeField] private string Name;
        [SerializeField] private float Speed;
        [SerializeField] private bool IsPredator;
        [SerializeField] private List<int> Colors;

        public AnimalInfoSimple(int iD, string name, float speed, bool isPredator, List<int> colors)
        {
            ID = iD;
            Name = name;
            Speed = speed;
            IsPredator = isPredator;
            Colors = colors;
        }

        public override string ToString()
        {
            return $"{ID} {Name} {Speed} {IsPredator} {string.Join("/", Colors)}";
        }
    }

    [Serializable]
    public class AnimalInfo
    {
        [SerializeField] private AnimalKey key;
        [SerializeField] private float speed;
        [SerializeField] private bool isPredator;
        [SerializeField] private List<int> colors;

        public AnimalKey Key => key;
        public float Speed  => speed;
        public bool IsPredator => isPredator;
        public List<int> Colosr => colors;

        public AnimalInfo(AnimalKey key, float speed, bool isPredator, List<int> colors)
        {
            this.key = key;
            this.speed = speed;
            this.isPredator = isPredator;
            this.colors = colors;
        }

        public override string ToString()
        {
            return $"{key} {speed} {IsPredator} {string.Join("/", colors)}";
        }
    }

    [Serializable]
    public struct AnimalKey
    {
        [SerializeField] private int id;
        [SerializeField] private string name;
        public int ID => id; 
        public string Name  => name;

        public AnimalKey(int id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public override string ToString()
        {
            return $"{id}>{name}";
        }
    }
}