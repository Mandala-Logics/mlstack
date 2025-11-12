using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace mlStringValidation.Path
{
    public enum PathProximityType { Unknown, Local, Network, Remote }

    public sealed class PathStructure : IEquatable<PathStructure>
    {
        //PROPERTIES
        public string StartToken { get; }
        public string FirstSeperator => firstSepArray?[0] ?? throw new NullReferenceException("First seperator is not defined.");
        public string Seperator => seperatorArray?[0] ?? throw new NullReferenceException("Seperator is not defined.");
        public string PathTypeName { get; }
        public int FirstElementLength { get; }
        public bool AllowOnlyStartToken { get; }
        public int RootLength { get; }
        public ConstructorInfo DefaultConstructor { get; }
        public PathProximityType PathProximity { get; }

        //BOOLS
        public bool HasStartToken => StartToken is object;
        public bool HasFirstSeperator => firstSepArray is object;
        public bool HasFixedFirstElementLen => FirstElementLength > 0;

        //PRIVATE PROPERTIES
        private readonly string[] firstSepArray;
        private readonly string[] seperatorArray;

        //CONSTRUCTORS
        public PathStructure(string pathTypeName, Type owningType, string seperator, int firstElementLen = 0, string startToken = null, string firstSeperator = null,
            bool allowJustStartToken = false, int rootLength = 1, PathProximityType proximity = PathProximityType.Local)
        {
            StartToken = startToken;
            PathTypeName = pathTypeName ?? throw new ArgumentNullException("pathTypeName");
            FirstElementLength = firstElementLen;
            AllowOnlyStartToken = allowJustStartToken;

            DefaultConstructor = owningType.GetConstructor(new Type[] { typeof(string), typeof(DestType) });

            if (DefaultConstructor is null || !DefaultConstructor.IsPublic) throw new PathImplimentationException("Each implimentation of pathbase must offer a public constrcutor with params (string, DestType).");

            if (AllowOnlyStartToken && string.IsNullOrWhiteSpace(StartToken))
            {
                throw new ArgumentException("Cannot allow a path to be only start token if no start token is declared.");
            }

            if (string.IsNullOrWhiteSpace(firstSeperator)) { firstSepArray = null; }
            else { firstSepArray = new string[] { firstSeperator }; }

            seperatorArray = new string[] { seperator ?? throw new ArgumentNullException("seperator") };

            PathProximity = proximity;
            RootLength = rootLength;
        }

        //PUBIC FUNCTIONS
        public PathBase ConstructPath(List<string> elements, PathType pathType) => ConstructPath(elements, pathType, DestType.Unknown);
        public PathBase ConstructPath(List<string> elements, PathType pathType, DestType mode)
        {
            bool absPath = pathType == PathType.Absolute || pathType == PathType.Unknown;

            if (elements.Count == 0)
            {
                if (absPath)
                {
                    if (AllowOnlyStartToken) { return ConstructPath(StartToken, mode); }
                    else { throw new PathParsingException("Path elements cannot be empty while AllowOnlyStartToken is false and/or path type is realitive."); }
                }
                else { return ConstructPath(elements[0], mode); }
            }
            else
            {
                StringBuilder sb = new StringBuilder(elements.Count * 2);                

                if (absPath)
                {
                    if (HasFixedFirstElementLen && elements[0].Length != FirstElementLength) throw new PathParsingException($"The first element length of this path {elements[0].Length} is not the same as the structre's first element length {FirstElementLength}.");

                    if (HasStartToken) sb.Append(StartToken);

                    sb.Append(elements[0]);

                    if (HasFirstSeperator) sb.Append(FirstSeperator);
                }
                else
                {
                    sb.Append(elements[0]);
                    if (elements.Count > 1) sb.Append(Seperator);
                }

                for (int x = 1; x < elements.Count - 1; x++)
                {
                    sb.Append(elements[x]);
                    sb.Append(Seperator);
                }

                if (elements.Count > 1) sb.Append(elements[elements.Count - 1]);

                return ConstructPath(sb.ToString(), mode);
            }
        }
        public PathBase ConstructPath(string path) => ConstructPath(path, DestType.Unknown);
        public PathBase ConstructPath(string path, DestType mode)
        {
            try
            {
                return (PathBase)DefaultConstructor.Invoke(new object[] { path, mode });
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is PathException)
                {
                    throw e.InnerException;
                }
                else { throw e; }
            }
        }
        public PathParsingException CatchValidate(string path, bool mustExist, bool containerMustExist)
        {
            PathBase pb;

            try
            {
                pb = ConstructPath(path);
            }
            catch (PathParsingException e) { return e; }

            if (mustExist)
            {
                if (pb.Exists) return null;
                else return new PathParsingException($"Path {pb} does not exist.");
            }
            else if (containerMustExist)
            {
                if (pb.Count == 1) return new PathParsingException($"Path {pb} does not exist.");
                else if (pb.GetContainingDir().Exists) return null;
                else return new PathParsingException($"Containg path {pb.GetContainingDir()} does not exist.");
            }
            else
            {
                return null;
            }
        }
        public bool Validate(string path, bool mustExist, bool containerMustExist)
        {
            PathBase pb;

            try 
            {
                pb = ConstructPath(path); 
            }
            catch (PathParsingException) { return false; }

            if (mustExist) return pb.Exists;
            else if (containerMustExist) return pb.GetContainingDir().Exists;
            else return true;
        }        

        //INTERNAL FUNCTIONS
        internal string[] SplitFirstSeperator(string path)
        {
            if (!HasFirstSeperator)
            {
                throw new PathImplimentationException("No first seperator in this path structure.");
            }
            else
            {
                return path.Split(firstSepArray, StringSplitOptions.RemoveEmptyEntries);
            }
        }
        internal string[] SplitSeperator(string path)
        {
            if (seperatorArray is null) throw new PathImplimentationException("Path Strcture's seperator is not defined.");

            return path.Split(seperatorArray, StringSplitOptions.RemoveEmptyEntries);
        }

        //OBJECT OVERRIDES
        public override bool Equals(object obj)
        {
            return obj is PathStructure structure && Equals(structure);
        }
        public bool Equals(PathStructure other)
        {
            return StartToken == other.StartToken &&
                   PathTypeName == other.PathTypeName &&
                   FirstElementLength == other.FirstElementLength &&
                   AllowOnlyStartToken == other.AllowOnlyStartToken &&
                   EqualityComparer<string[]>.Default.Equals(firstSepArray, other.firstSepArray) &&
                   EqualityComparer<string[]>.Default.Equals(seperatorArray, other.seperatorArray);
        }
        public override int GetHashCode()
        {
            int hashCode = 2115705806;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(StartToken);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(PathTypeName);
            hashCode = hashCode * -1521134295 + FirstElementLength.GetHashCode();
            hashCode = hashCode * -1521134295 + AllowOnlyStartToken.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(firstSepArray);
            hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(seperatorArray);
            return hashCode;
        }
        public static bool operator ==(PathStructure left, PathStructure right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(PathStructure left, PathStructure right)
        {
            return !(left == right);
        }
    }
}
