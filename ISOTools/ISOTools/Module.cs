using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using IsoTools;

namespace IsoTools
{
    public class Module
    {
        public List<Unit> Roster { get; private set; }
        public MapDefinition Map { get; private set; }

        /// <summary>
        /// Private Constructor.
        /// </summary>
        private Module() {
            Roster = new List<Unit>();
        }

        /// <summary>
        /// Saves a complete module file.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="map"></param>
        /// <param name="roster"></param>
        public static void Save(string fileName, MapDefinition map, ICollection<Unit> roster)
        {
            if (fileName == null)
                return;
            using (var stream = new StreamWriter(fileName))
            {
                stream.WriteLine("<Module>");
                map.AppendMap(stream);

                stream.WriteLine("<Roster>");
                stream.WriteLine(roster.Count);
                foreach (Unit u in roster)
                    u.AppendUnit(stream);
                stream.WriteLine("</Roster>");

                stream.WriteLine("</Module>");
            }
        }

        /// <summary>
        /// Reads a Module from the give stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="iso"></param>
        /// <returns></returns>
        public static Module ReadModule(TextReader stream, Isometry iso)
        {
            Module mod = new Module();
            stream.ReadLine();  //  <Module>
            mod.Map = MapDefinition.ReadMap(stream, iso);
            stream.ReadLine();  //  <Roster>
            int numUnits = int.Parse(stream.ReadLine());
            for (int i = 0; i < numUnits; i++)
            {
                mod.Roster.Add(Unit.ReadUnit(stream));
            }
            stream.ReadLine();  //  </Roster>
            stream.ReadLine();  //  </Module>
            return mod;
        }

        /// <summary>
        /// Opens and returns a Module from stream.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="asString"> </param>
        /// <returns></returns>
        public static Module OpenModule(string filePath, Isometry iso, bool asString)
        {
            TextReader stream;
            if (asString)
            {
                using (stream = new StringReader(filePath))
                {
                    return ReadModule(stream, iso);
                }
            }
            else
            {
                using (stream = new StreamReader(filePath))
                {
                    return ReadModule(stream, iso);
                }
            }
        }
    }
}
