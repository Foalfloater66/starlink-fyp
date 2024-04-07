using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

namespace Orbits
{
    public class GroundstationCollection
    {
        private Dictionary<GameObject, string> groundstation2name = new Dictionary<GameObject, string>();
        private Dictionary<string, GameObject> name2groundstation = new Dictionary<string, GameObject>();

        /// <summary>
        /// Add a groundstation object to the collection of groundstations.
        ///
        /// No groundstations may share the same name nor the same object.
        /// </summary>
        /// <param name="gs_object">Unique groundstation GameObject.</param>
        /// <param name="gs_name">Unique groundstation name.</param>
        public void addGroundstation(GameObject gs_object, string gs_name)
        {
            if (groundstation2name.ContainsKey(gs_object))
            {
                throw new Exception("A groundstation with the same GameObject already exists.");
            }
            groundstation2name.Add(gs_object, gs_name);
            
            if (name2groundstation.ContainsKey(gs_name))
            {
                throw new Exception("A groundstation with the same name already exists.");
            }
            name2groundstation.Add(gs_name, gs_object);
        }

        /// <summary>
        /// TODO: docstring.
        /// </summary>
        /// <returns></returns>
        public List<GameObject> ToList()
        { 
            return name2groundstation.Values.ToList();
        }

        /// <summary>
        /// With the groundstation GameObject as key, get its name.
        /// </summary>
        /// <param name="gs_object">Unique groundstation GameObject.</param>
        public string this[GameObject gs_object] => groundstation2name[gs_object];

        /// <summary>
        /// With the groundstation name as key, get its object.
        /// </summary>
        /// <param name="gs_name">Unique groundstation name.</param>
        public GameObject this[string gs_name] => name2groundstation[gs_name];
    }
}