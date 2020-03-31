using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Project = EnvDTE.Project;

namespace ATech.Ring.Vsix.Components
{
    internal static class VsServicesExtensions
    {
        internal static T GetGlobalService<T>() where T : class
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (Package.GetGlobalService(typeof(T)) is T svc) return svc;
            throw new Exception($"Could not get VS Service {typeof(T)}");
        }

        internal static T Resolve<T>() where T : class
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return (T) ServiceProvider.GlobalProvider.GetService(typeof(T));
        }

        internal static bool IsOfKind(this Project p, string kind)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return p.Kind == kind;
        }

        internal static bool IsSolutionFolder(this Project p) => p.IsOfKind(SolutionFolderProjectKind);
        internal static bool IsNetCoreCSharp(this Project p) => p.IsOfKind(NetCoreCSharpProjectKind);
        internal static bool IsNetFrameworkCSharp(this Project p) => p.IsOfKind(NetFrameworkCSharpProjectKind);
        internal static bool IsUnloadedProject(this Project p) => p.IsOfKind(UnloadedProjectTypeGuid);

        internal static IEnumerable<Project> AllProjects(this Solution s)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return s.Projects.OfType<Project>().SelectMany(GetProjects);
            IEnumerable<Project> GetProjects(Project p)
            {
                return p.IsSolutionFolder() ? (from x in p.ProjectItems.OfType<ProjectItem>()
                                               where x.SubProject != null
                                               select x.SubProject).SelectMany(GetProjects) : new[] { p };
            }
        }

        internal static bool TryGetProjectByUniqueName(this Solution s, string uniqueName, out Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            project = (from p in s.AllProjects() where p.UniqueName.Equals(uniqueName, StringComparison.OrdinalIgnoreCase) select p).SingleOrDefault();
            return project != null;
        }

        internal static void SetStartWebServerOnDebug(this Project project, bool value)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (project.Properties == null) return;

            foreach (var p in from p in project.Properties.OfType<Property>()
                              where p.Name != null && p.Name.Equals(StartWebServerOnDebug, StringComparison.OrdinalIgnoreCase)
                              select p)
            {
                try
                {
                    p.Value = value;
                    Debug.WriteLine($"Setting '{p.Name}' = '{value}'");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }

        private const string NetCoreCSharpProjectKind = "{9A19103F-16F7-4668-BE54-9A1E7A4F7556}";
        private const string NetFrameworkCSharpProjectKind = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
        private const string SolutionFolderProjectKind = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
        private const string UnloadedProjectTypeGuid = "{67294A52-A4F0-11D2-AA88-00C04F688DDE}";
        private const string StartWebServerOnDebug = "WebApplication.StartWebServerOnDebug";
    }
}