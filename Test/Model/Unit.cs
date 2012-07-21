﻿using System.Collections.Generic;
using System.Windows.Controls;
using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace Editor.Model
{
    public enum BodyType
    {
        Assault, BeastLord, BOSElder, BOSScribe, CitizenAlpha, CitizenMediumFemale, CitizenMediumMale, CitizenThinFemale, CitizenThinMale,
        Enclave, Environmental, Ghoul, GhoulArmour, Goliath, LeatherFemale, LeatherMale, MetalFemale, MetalMale,
        Mutant, MutantArmour, Omega, Pipboy, Power, RaiderFemale, RaiderMale, RaiderMaleHuge, RaiderMaleLarge, 
        ReaverFemale, ReaverMale, Sarge, TribalFemale, TribalMale, TribalMaleLarge, VaultFemale, VaultMale, WBOS
    }

    public enum WeaponType
    {
        None, Club, Heavy, Knife, Minigun, Pistol, Rifle, Rocket, SMG, Spear
    }

    public enum Stance
    {
        Stand, Crouch, Prone
    }

    public enum StatusEffect
    {
        Suppressed, Unconscious, Dead
    }

    /// <summary>
    /// State information for individual characters.
    /// </summary>
    public class Unit
    {
        private static byte _nextID;
        public static Dictionary<string, BitmapImage> Images { get; set; }

        public byte OwnerID { get; set; }
        public byte ID { get; set; }
        public BodyType Body { get; set; }
        public WeaponType Weapon { get; set; }
        public Stance Stance { get; set; }
        public byte InitialHitPoints { get; set; }
        public byte CurrentHitPoints { get; set; }
        public byte Expertise { get; set; }
        public ushort X { get; set; }
        public ushort Y { get; set; }
        public string Name { get; set; }
        public List<StatusEffect> Effects { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Unit(byte ownerid, BodyType body, WeaponType weapon, Stance stance, 
            byte iHP, byte cHP, byte expertise, ushort x, ushort y, string name, params StatusEffect[] effects )
        {
            OwnerID = ownerid;
            ID = _nextID;
            _nextID++;
            Body = body;
            Weapon = weapon;
            InitialHitPoints = iHP;
            CurrentHitPoints = cHP;
            Stance = stance;
            Expertise = expertise;
            X = x;
            Y = y;
            Name = name;
            Effects = new List<StatusEffect>();
            foreach(var se in effects)
            {
                Effects.Add(se);
            }
        }

        public BitmapImage Image
        {
            get
            {
                Console.WriteLine(System.AppDomain.CurrentDomain.BaseDirectory);
                if (Images == null)
                    Images = new Dictionary<string, BitmapImage>();

                string key = Enum.GetName(typeof(BodyType), Body) + "Stand" + Enum.GetName(typeof(WeaponType), Weapon);

                if (Images.ContainsKey(key))
                {
                    return Images[key];
                }
                else if (File.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "\\Images\\Characters\\" + key + ".png"))
                {
                    var bim = new BitmapImage(new Uri("\\Images\\Characters\\" + key + ".png"));
                    bim.CacheOption = BitmapCacheOption.OnLoad;
                    Images.Add(key, bim);
                    return bim;
                }

                key = Enum.GetName(typeof(BodyType), Body) + "Stand";
                
                if (Images.ContainsKey(key))
                    return Images[key];

                var bimi = new BitmapImage(new Uri("pack://application:,,,/Images/Characters/" + key + ".png"));
                bimi.CacheOption = BitmapCacheOption.OnLoad;
                Images.Add(key, bimi);
                return bimi;
            }
            private set { }
        }
        
    }
}
