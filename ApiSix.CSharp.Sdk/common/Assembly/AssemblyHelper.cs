using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ApiSix.CSharp
{
    public static class AssemblyHelper
    {
        private static List<Assembly> _referenceAssembly = new List<Assembly>();

        public static List<Assembly> GetAssemblies(params string[] virtualPaths)
        {
            var referenceAssemblies = new List<Assembly>();
            if (virtualPaths.Any())
            {
                referenceAssemblies = GetReferenceAssembly(virtualPaths);
            }
            else
            {
                var assemblyNames = GetDefaultAssemblyNames();//?.Select(p => p.Name).ToArray();
                assemblyNames = GetFilterAssemblies(assemblyNames);
                foreach (var name in assemblyNames)
                {
                    try
                    {
                        if (referenceAssemblies.Find(f => f.GetName().Name == name.Name) == null)
                            referenceAssemblies.Add(Assembly.Load(name));
                    }
                    catch (Exception ex)
                    {
                        XTrace.WriteLine(ex?.Message + ",GetAssemblies(params string[] virtualPaths) error " + name.FullName);
                    }
                }
                _referenceAssembly.AddRange(referenceAssemblies.Except(_referenceAssembly));
            }
            return referenceAssemblies;
        }
        private static IEnumerable<AssemblyName> GetDefaultAssemblyNames()
        {
            var refAssemblyNames = new List<AssemblyName>();
            //----------------------------------------------------------------------------------------------------
            var assemblyFiles = GetAllAssemblyFiles(AppDomain.CurrentDomain.BaseDirectory);
            foreach (var referencedAssemblyFile in assemblyFiles)
            {
                try
                {
                    var referencedAssembly = Assembly.LoadFrom(referencedAssemblyFile);
                    if (refAssemblyNames.Find(f => f.Name == referencedAssembly.GetName().Name) == null)
                        refAssemblyNames.Add(referencedAssembly.GetName());
                }
                catch (Exception ex)
                {
                    XTrace.WriteLine(ex?.Message + ",GetDefaultAssemblyNames() error " + referencedAssemblyFile);
                }
            }
            //----------------------------------------------------------------------------------------------------
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var a in assemblies)
            {
                if (refAssemblyNames.Find(f => f.Name == a.GetName().Name) == null)
                    refAssemblyNames.Add(a.GetName());

                var aa = a.GetReferencedAssemblies();
                foreach (var sas in aa)
                {
                    if (refAssemblyNames.Find(f => f.Name == sas.Name) == null)
                        refAssemblyNames.Add(sas);
                }
            }

            return refAssemblyNames;
        }
        private static List<Assembly> GetReferenceAssembly(params string[] virtualPaths)
        {
            var refAssemblies = new List<Assembly>();
            var rootPath = AppDomain.CurrentDomain.BaseDirectory;
            var existsPath = virtualPaths.Any();
            //if (existsPath && !string.IsNullOrEmpty(AppConfig.ServerOptions.RootPath))
            //   rootPath = AppConfig.ServerOptions.RootPath;
            var result = _referenceAssembly;
            if (!result.Any() || existsPath)
            {
                var paths = virtualPaths.Select(m => Path.Combine(rootPath, m)).ToList();
                if (!existsPath) paths.Add(rootPath);
                paths.ForEach(path =>
                {
                    var assemblyFiles = GetAllAssemblyFiles(path);
                    foreach (var referencedAssemblyFile in assemblyFiles)
                    {
                        try
                        {
                            var referencedAssembly = Assembly.LoadFrom(referencedAssemblyFile);
                            if (!_referenceAssembly.Contains(referencedAssembly))
                                _referenceAssembly.Add(referencedAssembly);
                            refAssemblies.Add(referencedAssembly);
                        }
                        catch (Exception ex)
                        {
                            XTrace.WriteLine(ex?.Message + ",GetReferenceAssembly(params string[] virtualPaths) error " + referencedAssemblyFile);
                        }
                    }
                    result = existsPath ? refAssemblies : _referenceAssembly;
                });
            }
            return result;
        }
        private static IEnumerable<AssemblyName> GetFilterAssemblies(IEnumerable<AssemblyName> assemblyNames)
        {
            var notRelatedFile = "";
            var relatedFile = "";
            var pattern = string.Format("^Microsoft.\\w*|^System.\\w*|^DotNetty.\\w*|^runtime.\\w*|^ZooKeeperNetEx\\w*"
                + "|^StackExchange.Redis\\w*|^Consul\\w*|^Newtonsoft.Json.\\w*|^Autofac.\\w*{0}",
               string.IsNullOrEmpty(notRelatedFile) ? "" : $"|{notRelatedFile}");
            Regex notRelatedRegex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Regex relatedRegex = new Regex(relatedFile, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!string.IsNullOrEmpty(relatedFile))
            {
                return
                    assemblyNames.Where(
                        name => !notRelatedRegex.IsMatch(name.Name) && relatedRegex.IsMatch(name.Name)).ToArray();
            }
            else
            {
                return
                    assemblyNames.Where(
                        name => !notRelatedRegex.IsMatch(name.Name)).ToArray();
            }
        }
        private static List<string> GetAllAssemblyFiles(string parentDir)
        {
            Directory.CreateDirectory(parentDir);

            var notRelatedFile = "";
            var relatedFile = "";
            var pattern = string.Format("^Microsoft.\\w*|^System.\\w*|^Netty.\\w*|^Autofac.\\w*|^libSkiaSharp.\\w*|^libuv.\\w*|^grpc_csharp_ext.\\w*{0}",
               string.IsNullOrEmpty(notRelatedFile) ? "" : $"|{notRelatedFile}");
            Regex notRelatedRegex = new Regex(pattern, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Regex relatedRegex = new Regex(relatedFile, RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
            if (!string.IsNullOrEmpty(relatedFile))
            {
                return
                    Directory.GetFiles(parentDir, "*.dll").Select(Path.GetFullPath).Where(
                        a => !notRelatedRegex.IsMatch(Path.GetFileName(a)) && relatedRegex.IsMatch(Path.GetFileName(a))).ToList();
            }
            else
            {
                return
                    Directory.GetFiles(parentDir, "*.dll").Select(Path.GetFullPath).Where(
                        a => !notRelatedRegex.IsMatch(Path.GetFileName(a))).ToList();
            }
        }



        public static TAttribute GetSingleAttributeOrDefaultByFullSearch<TAttribute>(TypeInfo info)
            where TAttribute : Attribute
        {
            var attributeType = typeof(TAttribute);
            if (info.IsDefined(attributeType, true))
            {
                return info.GetCustomAttributes(attributeType, true).Cast<TAttribute>().First();
            }
            else
            {
                foreach (var implInter in info.ImplementedInterfaces)
                {
                    var res = GetSingleAttributeOrDefaultByFullSearch<TAttribute>(implInter.GetTypeInfo());

                    if (res != null)
                    {
                        return res;
                    }
                }
            }

            return null;
        }

        public static TAttribute GetSingleAttributeOrDefault<TAttribute>(MemberInfo memberInfo, TAttribute defaultValue = default(TAttribute), bool inherit = true)
       where TAttribute : Attribute
        {
            var attributeType = typeof(TAttribute);
            if (memberInfo.IsDefined(typeof(TAttribute), inherit))
            {
                return memberInfo.GetCustomAttributes(attributeType, inherit).Cast<TAttribute>().First();
            }

            return defaultValue;
        }


        /// <summary>
        /// Gets a single attribute for a member.
        /// </summary>
        /// <typeparam name="TAttribute">Type of the attribute</typeparam>
        /// <param name="memberInfo">The member that will be checked for the attribute</param>
        /// <param name="inherit">Include inherited attributes</param>
        /// <returns>Returns the attribute object if found. Returns null if not found.</returns>
        public static TAttribute GetSingleAttributeOrNull<TAttribute>(this MemberInfo memberInfo, bool inherit = true)
            where TAttribute : Attribute
        {
            if (memberInfo == null)
            {
                throw new ArgumentNullException(nameof(memberInfo));
            }

            var attrs = memberInfo.GetCustomAttributes(typeof(TAttribute), inherit).ToArray();
            if (attrs.Length > 0)
            {
                return (TAttribute)attrs[0];
            }

            return default(TAttribute);
        }


        public static TAttribute GetSingleAttributeOfTypeOrBaseTypesOrNull<TAttribute>(this Type type, bool inherit = true)
            where TAttribute : Attribute
        {
            var attr = type.GetTypeInfo().GetSingleAttributeOrNull<TAttribute>();
            if (attr != null)
            {
                return attr;
            }

            if (type.GetTypeInfo().BaseType == null)
            {
                return null;
            }

            return type.GetTypeInfo().BaseType.GetSingleAttributeOfTypeOrBaseTypesOrNull<TAttribute>(inherit);
        }

    }

}
