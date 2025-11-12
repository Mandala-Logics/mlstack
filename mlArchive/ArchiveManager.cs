using System;
using System.Collections.Generic;
using System.Linq;
using DDLib;
using mlAutoCollection;
using mlThreadMGMT;

namespace ArcV4
{
    public static class ArchiveManager
    {
        //PUBLIC PROPERTIES
        public static IReadOnlyTypedAutoCollection<string, ArchiveV4> Archives { get; }

        //INTERNAL PROPERTIES
        internal static AutoCollection<ArchiveV4> Arcs { get; }

        //PRIVATE PROPERTIES
        private static bool disposing = false;

        //PRIVATE METHODS
        static ArchiveManager()
        {
            Arcs = new AutoCollection<ArchiveV4>(0, true, StringComparer.OrdinalIgnoreCase);
            Archives = Arcs.AsTypedReadOnly<string>();
        }

        //INTERNAL METHODS
        internal static void OnDisposed(ArchiveV4 arc)
        {
            if (!disposing) Arcs.Remove(arc);
        }

        //PUBLIC METHODS
        public static void DisposeAll()
        { 
            lock (Arcs.SyncRoot)
            {
                disposing = true;

                foreach (ArchiveV4 arc in Arcs) { arc.Dispose(); }

                Arcs.Clear();

                disposing = false;
            }            
        }
        public static List<ArcPath> GetRoots()
        {
            var ret = new List<ArcPath>(Arcs.Count);

            foreach (ArchiveV4 arc in Arcs)
            {
                ret.Add(arc.RootPath);
            }

            return ret;
        }
        public static IEnumerable<IThreadManager> GetThreadManagers() => Arcs.Cast<IThreadManager>();
    }
}
