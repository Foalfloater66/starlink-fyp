using System.Collections.Generic;
using Orbits.Satellites;
using Routing;
using UnityEngine;

namespace Scene
{
    // TODO: add documentation for ActiveISL and ActiveRF
    public class ActiveISL
    {
        public Satellite sat1, sat2;
        public Node node1, node2;
        public ActiveISL(Satellite sat1_, Node node1_, Satellite sat2_, Node node2_)
        {
            sat1 = sat1_;
            sat2 = sat2_;
            node1 = node1_;
            node2 = node2_;
        }
    }

    public class ActiveRF
    {
        public Satellite sat;
        public GameObject city;
        public Node node1, node2;
        public ActiveRF(GameObject city_, Node node1_, Satellite sat_, Node node2_)
        {
            city = city_;
            sat = sat_;
            node1 = node1_;
            node2 = node2_;
        }
    }

    public class ScenePainter
    {
        /// <value>
        /// Used Inter Satellite Links (ISL).
        /// </value>
        public List<ActiveISL> UsedISLLinks { get; set; }

        /// <value>
        /// Used Radio Frequency (RF) Links.
        /// </value>
        public List<ActiveRF> UsedRFLinks { get; set; }

        /// <value>
        /// Material for unused ISL links.
        /// </value>
        private Material _isl_material;

        /// <value>
        /// Material for ISL links used for routing.
        /// </value>
        private Material[] _laserMaterials;

        /// <value>
        /// Materials for target ISL links. 
        /// 
        /// The indexes indicate the status of the link:
        /// [0]: the link is still active.
        /// [1]: the link has been flooded.
        /// </value>
        private Material[] _target_link_materials;

        /// <value>
        /// Material for city objects.
        /// </value>
        private Material _cityMaterial;

        /// <summary>
        /// Constructor creating a <c>ScenePainter</c> object with new empty of <c>UsedISLLinks</c> and <c>UsedRFLinks</c> links.
        /// </summary>
        public ScenePainter(Material isl_material, Material[] laserMaterials, Material[] target_link_materials, Material cityMaterial)
        {
            UsedISLLinks = new List<ActiveISL>();
            UsedRFLinks = new List<ActiveRF>();
            _isl_material = isl_material;
            _laserMaterials = laserMaterials;
            _target_link_materials = target_link_materials;
            _cityMaterial = cityMaterial;
        }

        /// <summary> // TODO: can I describe this in more detail? I don't know what the purpose of the ChangeCityMaterial element is.
        /// Color a city object.
        /// </summary>
        /// <param name="city">City object</param>
        public void ChangeCityMaterial(GameObject city)
        {
            Renderer[] renderers = city.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.material = _cityMaterial;
            }
        }

        /// <summary>
        /// Color an ISL link with the chosen material.
        /// </summary>
        /// <param name="prevsat">Source satellite object.</param>
        /// <param name="sat">Destination satellite object.</param>
        /// <param name="prevnode">Source satellite node.</param>
        /// <param name="node">Destination satellite node.</param>
        /// <param name="mat">Material to color the link with.</param>
        private void ColorISLLink(Satellite prevsat, Satellite sat, Node prevnode, Node node, Material mat)
        {
            sat.ColorLink(prevsat, mat);
            prevsat.ColorLink(sat, mat);
            UsedISLLinks.Add(new ActiveISL(sat, node, prevsat, prevnode));
        }

        /// <summary>
        /// Color an ISL link that was turned on due to passing traffic.
        /// </summary>
        /// <param name="prevsat">Source satellite object.</param>
        /// <param name="sat">Destination satellite object.</param>
        /// <param name="prevnode">Source satellite node.</param>
        /// <param name="node">Destination satellite node.</param>
        public void ColorRouteISLLink(Satellite prevsat, Satellite sat, Node prevnode, Node node)
        {
            ColorISLLink(prevsat, sat, prevnode, node, _laserMaterials[2]);
        }

        /// <summary>
        /// Color the attacker's target ISL link based on whether it was flooded or not.
        /// </summary>
        /// <param name="prevsat">Source satellite object.</param>
        /// <param name="sat">Destination satellite object.</param>
        /// <param name="prevnode">Source satellite node.</param>
        /// <param name="node">Destination satellite node.</param>
        /// <param name="flooded">True if the link was flooded, false otherwise.</param>
        public void ColorTargetISLLink(Satellite prevsat, Satellite sat, Node prevnode, Node node, bool flooded)
        {
            if (flooded)
            {
                ColorISLLink(prevsat, sat, prevnode, node, _target_link_materials[1]);
            }
            else
            {
                ColorISLLink(prevsat, sat, prevnode, node, _target_link_materials[0]);
            }
        }

        /// <summary>
        /// Color a Radio Frequency Link.
        /// </summary>
        /// <param name="city">City connected to the RF link.</param>
        /// <param name="sat">Satellite connected to the RF link.</param>
        /// <param name="prevnode">City node.</param>
        /// <param name="node">Satellite node.</param>
        public void ColorRFLink(GameObject city, Satellite sat, Node prevnode, Node node)
        {
            sat.LinkOn(city);
            UsedRFLinks.Add(new ActiveRF(city, node, sat, prevnode));
        }

        /// <summary>
        /// Draw the lasers that were marked as coloured.
        /// </summary>
        /// <param name="satlist">List of satellites on the graph.</param>
        /// <param name="maxsats">Maximum number of satellites in the graph.</param>
        public void UpdateLasers(Satellite[] satlist, int maxsats, float speed)
        {
            UsedRFLinks.ForEach(a => a.sat.LinkOn(a.city));

            /* assign all the lasers that both sides agree on */
            for (int satnum = 0; satnum < maxsats; satnum++)
            {
                satlist[satnum].ClearAssignment();
            }

            for (int satnum = 0; satnum < maxsats; satnum++)
            {
                satlist[satnum].UsePreAssigned();
            }

            /* finalize the choices, and draw the lasers */
            for (int satnum = 0; satnum < maxsats; satnum++)
            {
                satlist[satnum].FinalizeLasers(speed, _isl_material);
            }
        }

        public void TurnLasersOff(Satellite[] satlist, int maxsats)
        {
            for (int satnum = 0; satnum < maxsats; satnum++)
            {
                satlist[satnum].LinkOff();
            }
        }


        /// <summary>
        /// Erase all ISL links from the scene.
        /// </summary>
        public void EraseAllISLLinks()
        {
            while (UsedISLLinks.Count > 0)
            {
                ActiveISL isl = UsedISLLinks[0];
                isl.sat1.ColorLink(isl.sat2, _isl_material);
                isl.sat2.ColorLink(isl.sat1, _isl_material);
                UsedISLLinks.RemoveAt(0);
            }
        }

        /// <summary>
        /// Erase all RF links from the scene.
        /// </summary>
        public void EraseAllRFLinks()
        {
            while (UsedRFLinks.Count > 0)
            {
                ActiveRF rf = UsedRFLinks[0];
                UsedRFLinks.RemoveAt(0);
            }
        }
    }
}