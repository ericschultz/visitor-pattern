﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace VisitorPattern.Automation
{
    /// <summary>
    /// 
    /// </summary>
    /// <from>https://github.com/PombeirP/T4Factories/blob/master/T4Factories.Testbed/CodeTemplates/VisualStudioAutomationHelper.ttinclude</from>
    /// <license>MIT</license>
    [CLSCompliant(false)]
    public class AutomationHelper
    {
        /// <summary>
        /// Creates a new instance of this class
        /// </summary>
        public AutomationHelper(DTE dte)
        {
            // store a reference to the template host
            // we will need this frequently
            Dte = dte;
        }

        public DTE Dte { get; set; }




        #region Solution and Projects
        /// <summary>
        /// Gets the full path of the solution file
        /// </summary>
        public string SolutionFile
        {
            get
            {
                return this.Dte.Solution.FileName;
            }
        }
        /// <summary>
        /// Gets the file name of the currently opened solution.
        /// </summary>
        public string SolutionFileName
        {
            get
            {
                return System.IO.Path.GetFileName(this.Dte.Solution.FileName);
            }
        }
        /// <summary>
        /// Gets the name of the currently opened solution
        /// </summary>
        public string SolutionName
        {
            get
            {
                return this.Dte.Solution.Properties.Item("Name").Value.ToString();
            }
        }

        /// <summary>
        /// Gets a list of all Projects within the solution
        /// </summary>
        public IEnumerable<EnvDTE.Project> GetAllProjects()
        {
            var ret = new List<EnvDTE.Project>();

            // take all projects that are at top level of the solution
            // and recursively search Project folders
            var topLevelProjects = this.Dte.Solution.Projects;

            foreach (EnvDTE.Project project in topLevelProjects)
            {
                if (project.Kind == vsProjectType.SolutionFolder)
                    ret.AddRange(GetProjectsFromItemsCollection(project.ProjectItems));
                else
                    ret.Add(project);
            }

            return ret;
        }
        /// <summary>
        /// Gets the project object within the current solution by a given project name.
        /// </summary>
        public EnvDTE.Project GetProject(string projectName)
        {
            return this.GetAllProjects()
                             .Where(p => p.Name == projectName)
                             .First();
        }
        /// <summary>

        #endregion

        #region Project Items
        public EnvDTE.ProjectItem FindProjectItem(string fileName)
        {
            return this.Dte.Solution.FindProjectItem(fileName);
        }
        /// <summary>
        /// Gets all project items from the current solution
        /// </summary>
        public IEnumerable<EnvDTE.ProjectItem> GetAllSolutionItems()
        {
            var ret = new List<EnvDTE.ProjectItem>();

            // iterate all projects and add their items
            foreach (EnvDTE.Project project in this.GetAllProjects())
                ret.AddRange(GetAllProjectItems(project));

            return ret;
        }

        /// <summary>
        /// Gets all Project items from a given project.
        /// </summary>
        public IEnumerable<EnvDTE.ProjectItem> GetAllProjectItems(EnvDTE.Project project)
        {
            return this.GetProjectItemsRecursively(project.ProjectItems);
        }
        #endregion

        #region Code Model
        /// <summary>
        /// Searches a given collection of CodeElements recursively for objects of the given elementType.
        /// </summary>
        /// <param name="elements">Collection of CodeElements to recursively search for matching objects in.</param>
        /// <param name="elementType">Objects of this CodeModelElement-Type will be returned.</param>
        /// <param name="includeExternalTypes">If set to true objects that are not part of this solution are retrieved, too. E.g. the INotifyPropertyChanged interface from the System.ComponentModel namespace.</param>
        /// <returns>A list of CodeElement objects matching the desired elementType.</returns>
        public List<EnvDTE.CodeElement> GetAllCodeElementsOfType(EnvDTE.CodeElements elements, EnvDTE.vsCMElement elementType, bool includeExternalTypes)
        {
            var ret = new List<EnvDTE.CodeElement>();

            foreach (EnvDTE.CodeElement elem in elements)
            {
                // iterate all namespaces (even if they are external)
                // > they might contain project code
                if (elem.Kind == EnvDTE.vsCMElement.vsCMElementNamespace)
                {
                    ret.AddRange(GetAllCodeElementsOfType(((EnvDTE.CodeNamespace)elem).Members, elementType, includeExternalTypes));
                }
                // if its not a namespace but external
                // > ignore it
                else if (elem.InfoLocation == EnvDTE.vsCMInfoLocation.vsCMInfoLocationExternal
                        && !includeExternalTypes)
                    continue;
                // if its from the project
                // > check its members
                else if (elem.IsCodeType)
                {
                    ret.AddRange(GetAllCodeElementsOfType(((EnvDTE.CodeType)elem).Members, elementType, includeExternalTypes));
                }

                // if this item is of the desired type
                // > store it
                if (elem.Kind == elementType)
                    ret.Add(elem);
            }

            return ret;
        }
        #endregion


        #region Auxiliary stuff
        private List<EnvDTE.Project> GetProjectsFromItemsCollection(EnvDTE.ProjectItems items)
        {
            var ret = new List<EnvDTE.Project>();

            foreach (EnvDTE.ProjectItem item in items)
            {
                if (item.SubProject == null)
                    continue;
                else if (item.SubProject.Kind == vsProjectType.SolutionFolder)
                    ret.AddRange(GetProjectsFromItemsCollection(item.SubProject.ProjectItems));
                else if (item.SubProject.Kind == vsProjectType.VisualBasic
                         || item.SubProject.Kind == vsProjectType.VisualCPlusPlus
                         || item.SubProject.Kind == vsProjectType.VisualCSharp
                         || item.SubProject.Kind == vsProjectType.VisualJSharp
                         || item.SubProject.Kind == vsProjectType.WebProject)
                    ret.Add(item.SubProject);
            }

            return ret;
        }
        private List<EnvDTE.ProjectItem> GetProjectItemsRecursively(EnvDTE.ProjectItems items)
        {
            var ret = new List<EnvDTE.ProjectItem>();
            if (items == null) return ret;

            foreach (EnvDTE.ProjectItem item in items)
            {
                ret.Add(item);
                ret.AddRange(GetProjectItemsRecursively(item.ProjectItems));
            }

            return ret;
        }
        #endregion

    }

    public class vsProjectType
    {
        public const string SolutionFolder = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";
        public const string VisualBasic = "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}";
        public const string VisualCSharp = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
        public const string VisualCPlusPlus = "{8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942}";
        public const string VisualJSharp = "{E6FDF86B-F3D1-11D4-8576-0002A516ECE8}";
        public const string WebProject = "{E24C65DC-7377-472b-9ABA-BC803B73C61A}";
    }
}
