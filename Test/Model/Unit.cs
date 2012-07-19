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
        Club, Heavy, Knife, Minigun, Pistol, Rifle, Rocket, SMG, Spear
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
        public byte OwnerID;
        public byte ID;
        public BodyType Body;
        public WeaponType Weapon;
        public Stance Stance;
        public byte InitialHitPoints;
        public byte CurrentHitPoints;
        public byte Expertise;
        public ushort X;
        public ushort Y;
        public string Name;
        public List<StatusEffect> Effects;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Unit(byte ownerid, byte id, BodyType body, WeaponType weapon, Stance stance, 
            byte iHP, byte cHP, byte expertise, ushort x, ushort y, string name, params StatusEffect[] effects )
        {
            OwnerID = ownerid;
            ID = id;
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
