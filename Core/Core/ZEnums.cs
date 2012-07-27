using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZarTools
{
    public enum BodyType
    {
        Assault, BeastLord, BOSElder, BOSScribe, CitizenAlpha, CitizenMediumFemale, CitizenMediumMale, CitizenThinFemale, CitizenThinMale,
        Enclave, Environmental, Ghoul, GhoulArmour, Goliath, LeatherFemale, LeatherMale, MetalFemale, MetalMale,
        Mutant, MutantArmour, Pipboy, Power, RaiderFemale, RaiderMale, RaiderMaleHuge, RaiderMaleLarge,
        ReaverFemale, ReaverMale, Sarge, TribalFemale, TribalMale, TribalMaleLarge, VaultFemale, VaultMale, WBOS
    }

    public enum WeaponType
    {
        None, Club, Heavy, Knife, Minigun, Pistol, Rifle, Rocket, SMG, Spear
    }

    public enum Stance
    {
        Stand, Crouch, Prone, Death
    }

    public enum AnimAction
    {
        Breathe, DodgeOne, DodgeTwo, Fallback, Fallenback, Fallenforward, Fallforward, Getupback, Getupforward,
        Walk, Run, Magic, Magichigh, Magiclow, Crouch, Stand, Prone, Recoil, Pickup, Climb, Climbup, Climbdown, None,
        Swing, Burst, Single, Slash, Throw, Thrust, UnarmedOne, UnarmedTwo,
        Death, DeathBighole, DeathCutinhalf, DeathElectrify, DeathExplode, DeathFire, DeathMelt, DeathRiddled,
    }

    public enum TileType
    {
        Wall, Floor, Object, Stairs, Roof, Unknown
    }

    public enum TileMaterial
    {
        Stone, Gravel, Metal, Wood, Water, Snow, Ladder, Unknown
    }

    public enum TileFlag
    {
        Ethereal, HurtsNPC, Window, NoShadow, Climbable, NoPop, Exit, Invisible
    }
}
