using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;

namespace Launcher
{
    struct Version
    {
        internal static Version init = new Version(0, 0, 0);

        private short major;
        private short minor;
        private short subMinor;

        internal Version(short _major, short _minor, short _subMinor) { this.major = _major; this.minor = _minor; this.subMinor = _subMinor; }
        internal Version(string _version)
        {
            string[] _versionstrs = _version.Split('.');
            if (_versionstrs.Length != 3) { major = 0; minor = 0; subMinor = 0; }
            if (_versionstrs[0].StartsWith("v")){
                _versionstrs[0] = _versionstrs[0].Substring(1);
            }
            major = short.Parse(_versionstrs[0]);
            minor = short.Parse(_versionstrs[1]);
            subMinor = short.Parse(_versionstrs[2]);
        }

        internal bool hasUpdate(Version _new_version)
        {
            if (major < _new_version.major) { return true; }
            else if (major > _new_version.major) { return false; }
            else
            {
                if (minor < _new_version.minor) { return true; }
                else if (minor > _new_version.minor) { return false; }
                else
                {
                    if (subMinor < _new_version.subMinor) { return true; }
                    else if (subMinor > _new_version.subMinor) { return false; }
                }
            }

            return false;
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{subMinor}";
        }
        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj == null)
            {
                return false;
            }

            try
            {
                var anotherVersion = (Version)obj;

                return major == anotherVersion.major && minor == anotherVersion.minor && subMinor == anotherVersion.subMinor;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public static bool operator ==(Version v1, Version v2)
        {
            return v1.Equals(v2);
        }

        public static bool operator !=(Version v1, Version v2)
        {
            return !v1.Equals(v2);
        }

        public static bool operator >(Version v1, Version v2)
        {
            if (v1.major > v2.major) { return true; }
            else if (v1.major < v2.major) { return false; }
            else
            {
                if (v1.minor > v2.minor) { return true; }
                else if (v1.minor < v2.minor) { return false; }
                else
                {
                    if (v1.subMinor > v2.subMinor) { return true; }
                    else if (v1.subMinor < v2.subMinor) { return false; }
                }
            }
            return false;
        }

        public static bool operator <(Version v1, Version v2)
        {
            if (v1.major < v2.major) { return true; }
            else if (v1.major > v2.major) { return false; }
            else
            {
                if (v1.minor < v2.minor) { return true; }
                else if (v1.minor > v2.minor) { return false; }
                else
                {
                    if (v1.subMinor < v2.subMinor) { return true; }
                    else if (v1.subMinor > v2.subMinor) { return false; }
                }
            }
            return false;
        }

        public static bool operator >=(Version v1, Version v2)
        {
            return v1.Equals(v2) || v1 > v2;
        }

        public static bool operator <=(Version v1, Version v2)
        {
            return !v1.Equals(v2) || v1 < v2;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }


    }
}
