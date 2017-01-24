﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ============================================================
//  Made by Léo alias "The Warlod of the Eight Peaks, Scarsnik"
// ============================================================

namespace Galt.Crawler.Util
{
    public class GraphData
    {
        Dictionary<string, object> _info;
        Dictionary<string, List<Dictionary<string, string>>> _graph;

        // Convert the given VPackage in a JSON object with all the informations needed
        public Dictionary<string, object> ConvertGraphData(VPackage vPackage)
        {
            _graph = new Dictionary<string, List<Dictionary<string, string>>>();
            _graph.Add("nodes", new List<Dictionary<string, string>>());
            _graph.Add("links", new List<Dictionary<string, string>>());

            _info = new Dictionary<string, object>();
            _info.Add("graph", _graph);
            _info.Add("versionConflict", new List<Dictionary<string, string>>());
            _info.Add("toUpdate", new List<Dictionary<string, string>>());

            _graph["nodes"].Add(VPackageToDictionary(vPackage.PackageId, _graph["nodes"].Count.ToString(), "source", vPackage.Version.ToString(), vPackage.LastVersion));
            AddDependency(vPackage, "0");

            // Add the warnings on nodes withs version conflict
            foreach (Dictionary<string, string> currentNode in _graph["nodes"])
            {
                foreach (Dictionary<string, string> otherNode in _graph["nodes"])
                {
                    if (currentNode["name"] == otherNode["name"]
                        && currentNode["id"] != otherNode["id"]
                        && currentNode.ContainsKey("version")
                        && currentNode["version"] != otherNode["version"])
                    {
                        if ((currentNode.ContainsKey("entity") && currentNode["entity"] != "platform")||!currentNode.ContainsKey("entity"))
                        {
                            if (!currentNode.Keys.Contains("warning") && !otherNode.Keys.Contains("warning"))
                            {
                                currentNode.Add("warning", "versionConflict");
                                otherNode.Add("warning", "versionConflict");
                            }
                            else
                            {
                                currentNode["warning"] = "versionConflict";
                                otherNode["warning"] = "versionConflict";
                            }

                            // Adding all version conflicts in the list of issues
                            bool contains = false;
                            foreach (Dictionary<string, string> dic in (List<Dictionary<string, string>>)_info["versionConflict"])
                            {
                                if (dic["name"] == currentNode["name"])
                                {
                                    contains = true;
                                    string[] list = dic["versions"].Split();
                                    if (!list.Contains(currentNode["version"])) dic["versions"] = dic["versions"] + ", " + currentNode["version"];
                                }
                            }
                            if (!contains)
                            {
                                List<Dictionary<string, string>> toUpdate = (List<Dictionary<string, string>>)_info["versionConflict"];
                                toUpdate.Add(new Dictionary<string, string>());
                                toUpdate[toUpdate.Count - 1].Add("name", currentNode["name"]);
                                toUpdate[toUpdate.Count - 1].Add("versions", currentNode["version"]);
                            }
                        }
                    }
                }
            }
            return _info;
        }

        // Add dependencies of the given VPackage in the JSON object
        private void AddDependency(VPackage vPackage, string ParentId)
        {
            string id = "0";
            foreach (string framework in vPackage.Dependencies.Keys)
            {
                id = _graph["nodes"].Count.ToString();
                if (vPackage.Dependencies[framework].Count() != 0 && framework != "Unsupported,Version=v0.0")
                {
                    _graph["nodes"].Add(VPackageToDictionary(framework, id, "platform", vPackage.Version.ToString(), null));
                    _graph["links"].Add(CreateLink(ParentId, id));
                    ParentId = id;
                }

                // Adding of all the package in the framework. Ignore present package
                foreach (VPackage newVPackage in vPackage.Dependencies[framework])
                {
                    id = _graph["nodes"].Count.ToString();
                    bool found = false;
                    string idFound = "0";

                    foreach(Dictionary<string, string> node in _graph["nodes"])
                    {
                        if (node["name"] == newVPackage.PackageId && node["version"] == newVPackage.Version.ToString())
                        {
                            found = true;
                            idFound = node["id"];
                        }
                    }

                    if (!found)
                    {
                        _graph["nodes"].Add(VPackageToDictionary(newVPackage.PackageId, id, null, newVPackage.Version.ToString(), newVPackage.LastVersion));
                        _graph["links"].Add(CreateLink(ParentId, id));
                        AddDependency(newVPackage, id);
                    }
                    else
                    {
                        _graph["links"].Add(CreateLink(ParentId, idFound));
                    }
                    
                }
            }
        }

        // Convert create a Dictionary for a node
        private Dictionary<string, string> VPackageToDictionary(string name, string newId, string entity, string version, string lastVersion)
        {
            Dictionary<string, string> dico = new Dictionary<string, string>();

            dico.Add("id", newId);
            dico.Add("name", name);
            if (!String.IsNullOrWhiteSpace(entity))
                dico.Add("entity", entity);
            if (!String.IsNullOrWhiteSpace(version) && entity != "platform")
                dico.Add("version", version);

            if (entity != "platform" && version != lastVersion)
            {
                //dico.Add("warning", "toUpdate");
                bool contains = false;
                foreach (Dictionary<string, string> dic in (List<Dictionary<string, string>>) _info["toUpdate"])
                {
                    if (dic["name"] == name) contains = true;
                }

                if (!contains)
                {
                    List<Dictionary<string, string>> toUpdate = (List<Dictionary<string, string>>)_info["toUpdate"];
                    toUpdate.Add(new Dictionary<string, string>());
                    toUpdate[toUpdate.Count - 1].Add("name", name);
                    toUpdate[toUpdate.Count-1].Add("currentVersion", version);
                    toUpdate[toUpdate.Count-1].Add("lastVersion", lastVersion);
                }
            }

            return dico;
        }

        //Create a Dictionnary for a link
        private Dictionary<string, string> CreateLink(string source, string target)
        {
            Dictionary<string, string> dico = new Dictionary<string, string>();

            dico.Add("source", source);
            dico.Add("target", target);

            return dico;
        }

        public Dictionary<string, List<Dictionary<string, string>>> Graph
        {
            get { return _graph; }
        }
    }
}