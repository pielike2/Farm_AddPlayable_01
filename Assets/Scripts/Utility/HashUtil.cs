namespace Utility
{
    public static class HashUtil
    {
        public static int StringToHash(string s)
        {
            if (string.IsNullOrEmpty(s))
                return 0;
            int hash = 17;
            for (int i = 0; i < s.Length; i++)
                hash = hash * 31 + s[i];
            return hash;
        }
    }
}