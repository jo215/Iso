using System.Collections.Generic;

namespace Editor.Model
{
    public enum BodyType
    {
        Assault, BeastLord, BOSElder, BOSScribe, CitizenAlpha, CitizenMediumFemale, CitizenMediumMale, CitizenThinFemale, CitizenThinMale,
        DeathClaw, DeathClawBaby, Dummy, Enclave, Environmental, Ghoul, GhoulArmour, Goliath, LeatherFemale, LeatherMale, MetalFemale, MetalMale,
        Mutant, MutantArmour, MutantFreak, Omega, Pipboy, Power, RaiderFemale, RaiderMale, RaiderMaleHuge, RaiderMaleLarge, 
        ReaverFemale, ReaverMale, Sarge, TreadmillMan, TribalFemale, TribalMale, TribalMaleLarge, VaultFemale, VaultMale, WBOS
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
    }
}
