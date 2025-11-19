using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Nolvus.Package.Conditions;
using Nolvus.Core.Services;

namespace Nolvus.Package.Rules
{
    public abstract class Rule
    {
        public List<RuleCondition> Conditions = new List<RuleCondition>();

        public bool Force { get; set; }

        public virtual bool IsPriority => false;

        public virtual void Load(XmlNode node)
        {
            Force = false;
            Conditions.Clear();

            if (node["Force"] != null)
                Force = Convert.ToBoolean(node["Force"].InnerText);

            XmlNode conditionsNode = node.ChildNodes
                .Cast<XmlNode>()
                .FirstOrDefault(x => x.Name == "Conditions");

            if (conditionsNode != null)
            {
                foreach (XmlNode conditionNode in conditionsNode.ChildNodes)
                {
                    try
                    {
                        string typeName = conditionNode["Type"]?.InnerText;

                        if (string.IsNullOrWhiteSpace(typeName))
                            continue;

                        // Use assembly-qualified name - required on Linux
                        Type t = Type.GetType(
                            $"Nolvus.Package.Conditions.{typeName}, Nolvus.Package",
                            throwOnError: false
                        );

                        if (t == null)
                        {
                            ServiceSingleton.Logger.Log(
                                $"Rule: Unable to resolve condition type '{typeName}'"
                            );
                            continue;
                        }

                        RuleCondition cond = Activator.CreateInstance(t) as RuleCondition;

                        if (cond == null)
                        {
                            ServiceSingleton.Logger.Log(
                                $"Rule: Failed to instantiate condition '{typeName}'"
                            );
                            continue;
                        }

                        cond.Load(conditionNode);
                        Conditions.Add(cond);
                    }
                    catch (Exception ex)
                    {
                        ServiceSingleton.Logger.Log(
                            $"Rule: Failed to load condition: {ex.Message}"
                        );
                    }
                }
            }
        }

        public abstract void Execute(string gamePath, string extractDir, string modDir, string instanceDir);

        protected virtual bool CanExecute(string gamePath, string installDir)
        {
            foreach (var cond in Conditions)
            {
                if (!cond.IsValid(gamePath, installDir))
                    return false;
            }

            return true;
        }

        // Shared path normalization helper
        protected string Normalize(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return string.Empty;

            path = path.Replace("\\", "/");

            while (path.StartsWith("/"))
                path = path.TrimStart('/');

            return path;
        }
    }
}
