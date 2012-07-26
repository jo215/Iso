using System.Collections.Generic;
using System.Windows.Controls;
using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using ZarTools;
using ISOTools;

namespace Editor.Model
{

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
        public static Dictionary<BitmapImage, Bitmap> Bitmaps { get; set; }

        public byte OwnerID { get; set; }
        public byte ID { get; set; }

        public BodyType Body { get; set; }
        public WeaponType Weapon { get; set; }
        public Stance Stance { get; set; }
        public CompassDirection Facing { get; set; }

        public byte InitialHitPoints { get; set; }
        public byte CurrentHitPoints { get; set; }
        public byte InitialActionPoints { get; set; }
        public byte CurrentActionPoints { get; set; }
        public byte Expertise { get; set; }

        public short X { get; set; }
        public short Y { get; set; }

        public string Name { get; set; }
        public List<StatusEffect> Effects { get; set; }

        public ZSprite Sprite { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Unit(byte ownerid, BodyType body, WeaponType weapon, Stance stance, 
            byte iHP, byte cHP, byte expertise, byte iAP, byte cAP, short x, short y, string name, params StatusEffect[] effects )
        {
            OwnerID = ownerid;
            ID = _nextID;
            _nextID++;
            Body = body;
            Weapon = weapon;
            InitialHitPoints = iHP;
            CurrentHitPoints = cHP;
            InitialActionPoints = iAP;
            CurrentActionPoints = cAP;
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

        /// <summary>
        /// Opens and returns a Unit from stream.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="asString"> </param>
        /// <returns></returns>
        public static Unit OpenUnit(string filePath, bool asString)
        {
            if (filePath == null)
                return new Unit(0, BodyType.MetalMale, WeaponType.Rifle, Stance.Stand, 1, 1, 1, 1, 1, 1, 1, "Biff");

            TextReader stream;
            if (asString)
            {
                using (stream = new StringReader(filePath))
                {
                    return ReadUnit(stream);
                }
            }
            else
            {
                using (stream = new StreamReader(filePath))
                {
                    return ReadUnit(stream);
                }
            }
        }

        /// <summary>
        /// Reads a Unit from the given stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static Unit ReadUnit(TextReader stream)
        {
            var unit = new Unit(0, BodyType.MetalMale, WeaponType.Rifle, Stance.Stand, 1, 1, 1, 1, 1, 1, 1, "Biff");

            if (stream == null)
                return unit;

            stream.ReadLine(); // <Unit>
            unit.OwnerID = byte.Parse(stream.ReadLine());
            unit.Body = (BodyType)Enum.Parse(typeof(BodyType), stream.ReadLine());
            unit.Weapon = (WeaponType)Enum.Parse(typeof(WeaponType), stream.ReadLine());
            unit.Stance = (Stance)Enum.Parse(typeof(Stance), stream.ReadLine());
            unit.Facing = (CompassDirection)Enum.Parse(typeof(CompassDirection), stream.ReadLine());
            unit.InitialHitPoints = byte.Parse(stream.ReadLine());
            unit.CurrentHitPoints = byte.Parse(stream.ReadLine());
            unit.InitialActionPoints = byte.Parse(stream.ReadLine());
            unit.CurrentActionPoints = byte.Parse(stream.ReadLine());
            unit.Expertise = byte.Parse(stream.ReadLine());
            unit.X = short.Parse(stream.ReadLine());
            unit.Y = short.Parse(stream.ReadLine());
            unit.Name = stream.ReadLine();

            string s;
            while ((s = stream.ReadLine()) != "</Unit>")
            {
                unit.Effects.Add((StatusEffect)Enum.Parse(typeof(StatusEffect), s));
            }// </Unit>
            return unit;
        }

        /// <summary>
        /// Saves this unit to a new file.
        /// </summary>
        public void SaveUnit(string filePath)
        {
            if (filePath == null)
                return;
            using (var stream = new StreamWriter(filePath))
            {
                AppendUnit(stream);
            }
        }

        /// <summary>
        /// Appends this Unit to the given stream.
        /// </summary>
        /// <param name="stream"></param>
        public void AppendUnit(TextWriter stream)
        {
            if (stream == null)
                return;
            //  Unit info
            stream.WriteLine("<Unit>");
            stream.WriteLine(OwnerID);
            stream.WriteLine(Enum.GetName(typeof(BodyType), Body));
            stream.WriteLine(Enum.GetName(typeof(WeaponType), Weapon));
            stream.WriteLine(Enum.GetName(typeof(Stance), Stance));
            stream.WriteLine(Enum.GetName(typeof(CompassDirection), Facing));
            stream.WriteLine(InitialHitPoints);
            stream.WriteLine(CurrentHitPoints);
            stream.WriteLine(InitialActionPoints);
            stream.WriteLine(CurrentActionPoints);
            stream.WriteLine(Expertise);
            stream.WriteLine(X);
            stream.WriteLine(Y);
            stream.WriteLine(Name);
            foreach (StatusEffect eff in Effects)
                stream.WriteLine(eff);
            stream.WriteLine("</Unit>");
        }

        /// <summary>
        /// Gets the placeholder BitmapImage. Although we cache, WPF requires a reload when accessing from a different thread than the creator.
        /// </summary>
        internal BitmapImage Image
        {
            get
            {
                //Console.WriteLine(System.AppDomain.CurrentDomain.BaseDirectory);
                if (Images == null)
                    Images = new Dictionary<string, BitmapImage>();

                string key = Enum.GetName(typeof(BodyType), Body) + "Stand" + Enum.GetName(typeof(WeaponType), Weapon);

                if (File.Exists(System.AppDomain.CurrentDomain.BaseDirectory + "\\Images\\Characters\\" + key + ".png"))
                {
                    var bim = new BitmapImage(new Uri("pack://application:,,,/Images/Characters/" + key + ".png"));
                    bim.CacheOption = BitmapCacheOption.OnLoad;
                    if (!Images.ContainsKey(key))
                        Images.Add(key, bim);
                    return bim;
                }

                key = Enum.GetName(typeof(BodyType), Body) + "Stand";
                

                var bimi = new BitmapImage(new Uri("pack://application:,,,/Images/Characters/" + key + ".png"));
                bimi.CacheOption = BitmapCacheOption.OnLoad;
                if (!Images.ContainsKey(key))
                    Images.Add(key, bimi);

                return bimi;
            }
            private set { }
        }

        /// <summary>
        /// Gets the placeholder Bitmap.
        /// </summary>
        internal Bitmap Bitmap
        {
            get
            {
                if (Bitmaps == null)
                    Bitmaps = new Dictionary<BitmapImage, Bitmap>();

                if (Bitmaps.ContainsKey(Image))
                    return Bitmaps[Image];

                Bitmap bmp = new Bitmap(Image.PixelWidth, Image.PixelHeight, PixelFormat.Format32bppPArgb);
                BitmapData data = bmp.LockBits(new Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);
                Image.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
                bmp.UnlockBits(data);
                Bitmaps.Add(Image, bmp);
                return bmp;
            }
            private set { }
        }
    }
}
