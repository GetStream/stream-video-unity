namespace Tests.Editor
{
    /// <summary>
    /// This implementation defines a very simple comparison
    /// between two FileInfo objects. It only compares the name
    /// of the files being compared and their length in bytes.  
    /// </summary>
    internal class SimpleFileCompare : System.Collections.Generic.IEqualityComparer<System.IO.FileInfo>
    {
        public bool Equals(System.IO.FileInfo f1, System.IO.FileInfo f2)
        {
            return (f1.Name == f2.Name &&
                    f1.Length == f2.Length);
        }

        // Return a hash that reflects the comparison criteria. According to the
        // rules for IEqualityComparer<T>, if Equals is true, then the hash codes must  
        // also be equal. Because equality as defined here is a simple value equality, not  
        // reference identity, it is possible that two or more objects will produce the same  
        // hash code.  
        public int GetHashCode(System.IO.FileInfo fi)
        {
            var s = $"{fi.Name}{fi.Length}";
            return s.GetHashCode();
        }
    }
}