using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CRH.Framework.Common
{
    public static class Infos
    {
        public const string NAME          = "CRHack Framework";
        public const string INTERNAL_NAME = "CRH.Framework";
        public const string COPYRIGHT = "(c) 2015 by CRHack Crew (crhack.romhack.org)";

        public static readonly string[] AUTHORS = new string[] { "kipy" };
        public static readonly string[] CONTRIBUTORS = new string[] { };

        /// <summary>
        /// Get framework informations
        /// </summary>
        public static string About()
        {
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            return String.Format
            (
                "{0} " +
                "version {1}.{2} (Build ID : {3}.{4})\n" +
                "{5}\n" +
                "Developped by {6}\n" +
                (CONTRIBUTORS.Length > 0 ? "Contributors : {7}\n" : "{7}"),

                INTERNAL_NAME,
                version.Major.ToString(), version.Minor.ToString(), version.Build.ToString(), version.Revision.ToString(),
                COPYRIGHT,
                String.Join(",  ", AUTHORS),
                CONTRIBUTORS.Length > 0 ? String.Join(",  ", CONTRIBUTORS) : ""
            );
        }
    }
}
