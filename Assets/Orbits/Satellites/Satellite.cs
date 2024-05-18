using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Orbits.Satellites
{
    public class Satellite
    {
        private int maxsats;
        private int phase1_satcount;
        private int maxlasers;
        public int satid;
        private int satnum;
        private int orbitnum;
        private double sat_phase_stagger; // 5/32 or 17/32 for phase 1
        private int sats_per_orbit; // 50 for phase 1
        private int orbital_planes; // 32 for phase 1
        private float altitude;
        private const int LASERS_PER_SAT = 4;
        public GameObject gameobject;
        private GameObject[] lasers;

        private Satellite[] laserdsts;
        private double[] lasertimes;
        private bool[] laseron;

        private Satellite[] nearestsats;
        private int nearestcount;

        public Satellite[] assignedsats;
        public int assignedcount;


        private Satellite[] preassignedsats; // pre, not prev
        private int preassignedcount;

        private Satellite[] prevassignedsats; // prev, not pre
        private int prevassignedcount;

        private Satellite[] drawsats;
        private int drawcount;

        private bool _glow = true;
        private bool _beam = false;
        private int _linkson = 0;
        private int beam_angle = 0;
        private float beam_radius = 0;
        private GameObject orbit;
        private GameObject beam1, beam2;
        private GameObject beam_prefab1, beam_prefab2;
        private GameObject laser_prefab, thin_laser_prefab;
        private GameObject[] links;

        private int max_linknum = 2;
        private GameObject[] graphlinks;
        private bool graphon = false;
        private int graphcount = 0;
        private int maxgraph = -1;
        private Transform earth_transform;


        public int Orbit =>
            /* Returns the orbit ID, not the object! */
            orbitnum;

        public Satellite(int satelliteid, int satellitenum, int orbitnumber, Transform earth_transform_,
            GameObject orbit_, double orbitalangle, int maxlasercount, int maxsatcount, int phase1_satcount_,
            double sat_phase_stagger_, int sats_per_orbit_, int orbital_planes_,
            float altitude_, int beam_angle_, float beam_radius_, GameObject sat_prefab, GameObject beam_prefab1_,
            GameObject beam_prefab2_, GameObject laser_prefab_, GameObject thin_laser_prefab_)
        {
            orbit = orbit_;
            satid = satelliteid; /* globally unique satellite ID */
            satnum = satellitenum; /* satellite's position in its orbit */
            orbitnum = orbitnumber;
            altitude = altitude_;
            beam_angle = beam_angle_;
            beam_radius = beam_radius_;
            beam_prefab1 = beam_prefab1_;
            beam_prefab2 = beam_prefab2_;
            laser_prefab = laser_prefab_;
            thin_laser_prefab = thin_laser_prefab_;
            earth_transform = earth_transform_;

            maxsats = maxsatcount; /* total number of satellites */
            phase1_satcount = phase1_satcount_; // number of satellites in phase 1 
            // (will equal maxsats if only simulating phase 1)
            maxlasers = maxlasercount;
            sat_phase_stagger = sat_phase_stagger_;
            sats_per_orbit = sats_per_orbit_;
            orbital_planes = orbital_planes_;

            nearestsats = new Satellite[maxsats];
            nearestcount = 0;

            assignedsats = new Satellite[LASERS_PER_SAT];
            assignedcount = 0;

            prevassignedsats = new Satellite[LASERS_PER_SAT];
            prevassignedcount = 0;

            preassignedsats = new Satellite[LASERS_PER_SAT];
            preassignedcount = 0;

            var pos = earth_transform.position;
            pos.x += 1f;
            gameobject = Object.Instantiate(sat_prefab, pos, earth_transform.rotation);
            gameobject.transform.RotateAround(Vector3.zero, Vector3.up, (float)orbitalangle);
            gameobject.transform.SetParent(orbit.transform, false);

            // Create the script assigned to the Satellite object.
            var ss = (SatelliteScript)gameobject.GetComponent(typeof(SatelliteScript));
            ss.orbit_id = orbitnumber;
            ss.id = satelliteid;

            max_linknum = 100; // maximum number of links that a satellite can handle. 

            links = new GameObject[max_linknum];

            lasers = new GameObject[LASERS_PER_SAT];
            laserdsts = new Satellite[LASERS_PER_SAT]; // laser destinations
            lasertimes = new double[LASERS_PER_SAT];
            laseron = new bool[LASERS_PER_SAT];
            for (var lc = 0; lc < maxlasers; lc++)
            {
                lasers[lc] = Object.Instantiate(laser_prefab, position(),
                    gameobject.transform.rotation);
                lasers[lc].transform.SetParent(gameobject.transform, true);
                lasertimes[lc] = Time.time;
                laseron[lc] = false;
            }

            for (var linknum = 0; linknum < max_linknum; linknum++)
            {
                links[linknum] = Object.Instantiate(laser_prefab, position(),
                    gameobject.transform.rotation);
                links[linknum].transform.SetParent(gameobject.transform, true);
            }
        }

        // clear out all references so GC can work, delete game objects, prepare for deletion
        public void clearrefs()
        {
            for (var lc = 0; lc < maxlasers; lc++)
            {
                Object.Destroy(lasers[lc]);
                lasers[lc] = null;
            }

            for (var linknum = 0; linknum < max_linknum; linknum++)
            {
                Object.Destroy(links[linknum]);
                links[linknum] = null;
            }

            for (var satnum = 0; satnum < maxsats; satnum++) nearestsats[satnum] = null;

            for (var satnum = 0; satnum < LASERS_PER_SAT; satnum++)
            {
                assignedsats[satnum] = null;
                preassignedsats[satnum] = null;
                prevassignedsats[satnum] = null;
            }
        }

        public bool glow
        {
            set
            {
                _glow = value;
                for (var lc = 0; lc < maxlasers; lc++) lasertimes[lc] = Time.time;
            }
            get => _glow;
        }

        public void BeamOn()
        {
            if (_beam) return;
            _beam = true;

            var pos = earth_transform.position;

            beam1 = Object.Instantiate(beam_prefab1, pos, Quaternion.Euler(0f, -90f, 0f));
            beam1.transform.SetParent(gameobject.transform, false);

            // move the ring down to earth's surface.  0.99 is a spherical correction, as we need 
            // to center to be below ground and the edge on the ground
            var scale = orbit.transform.localScale;
            pos = gameobject.transform.position * 0.99f * 6371 / scale.x;
            beam1.transform.position = pos;
            // radius of circle for 25 degree beam/550km alt is 940km - adjust in GUI
            // radius of circle for 40 degree beam/550km alt is 573.5km
            // radius of circle for 40 degree beam/1150km alt is 1060km

            /* this is a very ugly way of changing the radius of the ring,
             * but you cannot directly change a particle system from the API */
            var ps = (ParticleSystem)beam1.GetComponent(typeof(ParticleSystem));
            var radius = ps.shape.radius;
            if (radius != beam_radius)
            {
                var sf = beam_radius / radius;
                ps.transform.localScale = new Vector3(sf, sf, sf);
            }

            pos = earth_transform.position;
            beam2 = Object.Instantiate(beam_prefab2, pos, Quaternion.Euler(0f, -90f, 0f));
            beam2.transform.SetParent(gameobject.transform, false);
            var light = (Light)beam2.GetComponent(typeof(Light));
            if (beam_angle == 25)
                light.spotAngle = 122f;
            else if (beam_angle == 40) light.spotAngle = 95f;
        }

        public void BeamOff()
        {
            if (!_beam) return;
            Object.Destroy(beam1);
            Object.Destroy(beam2);
            _beam = false;
        }

        public void LinkOn(GameObject city)
        {
            try
            {
                var ls = (LaserScript)links[_linkson].GetComponent(typeof(LaserScript));
                ls.SetPos(gameobject.transform.position, city.transform.position);
                _linkson++;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Exception thrown: {e}");
            }
        }

        public void LinkOff()
        {
            if (_linkson > 0)
            {
                for (var linknum = 0; linknum < max_linknum; linknum++)
                {
                    var ls = (LaserScript)links[linknum].GetComponent(typeof(LaserScript));
                    ls.SetPos(position(), position());
                }

                _linkson = 0;
            }
        }

        public void GraphOn(GameObject city, Material mat)
        {
            if (graphon == false)
            {
                graphlinks = new GameObject[1200];
                graphcount = 0;
                graphon = true;
            }

            var newlink = false;
            if (graphcount > maxgraph)
            {
                graphlinks[graphcount] =
                    Object.Instantiate(thin_laser_prefab, position(), gameobject.transform.rotation);
                graphlinks[graphcount].transform.SetParent(gameobject.transform, true);
                maxgraph = graphcount;
                newlink = true;
            }

            var ls = (LaserScript)graphlinks[graphcount].GetComponent(typeof(LaserScript));
            if (newlink) ls.line = ls.GetComponent<LineRenderer>();
            ls.SetPos(gameobject.transform.position, city.transform.position);
            if (mat != null) ls.ChangeMaterial(mat);
            graphcount++;
        }

        public void GraphReset()
        {
            graphcount = 0;
        }

        public void GraphDone()
        {
            for (var i = graphcount; i <= maxgraph; i++)
            {
                Object.Destroy(graphlinks[i]);
                graphlinks[i] = null;
            }

            maxgraph = graphcount - 1;
        }

        public Vector3 position()
        {
            return gameobject.transform.position;
        }

        public void AddSat(Satellite newsat)
        {
            nearestsats[nearestcount] = newsat;
            nearestcount++;
        }

        public void ChangeMaterial(Material newMat)
        {
            var renderers = gameobject.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.material = newMat;
        }

        public float Dist(Satellite s)
        {
            return Vector3.Distance(position(), s.position());
        }


        public bool IsAssigned(Satellite s) //remove the public modifier
        {
            for (var i = 0; i < assignedcount; i++)
                if (assignedsats[i] == s)
                    return true;
            return false;
        }

        public void ClearAssignment()
        {
            for (var i = 0; i < assignedcount; i++) prevassignedsats[i] = assignedsats[i];
            prevassignedcount = assignedcount;
            assignedcount = 0;
        }

        private bool WasAssigned(Satellite s)
        {
            for (var i = 0; i < prevassignedcount; i++)
                if (prevassignedsats[i] == s)
                    return true;
            return false;
        }

        public void SimpleAssign(Satellite s)
        {
            assignedsats[assignedcount] = s;
            assignedcount++;
        }

        private bool Assign(Satellite s)
        {
            if (assignedcount == LASERS_PER_SAT || s.assignedcount == LASERS_PER_SAT || IsAssigned(s)) return false;
            Debug.Assert(s.IsAssigned(this) == false);
            SimpleAssign(s);
            s.SimpleAssign(this);
            return true;
        }

        public void SimplePreAssign(Satellite s)
        {
            preassignedsats[preassignedcount] = s;
            preassignedcount++;
        }

        private bool IsPreAssigned(Satellite s)
        {
            for (var i = 0; i < preassignedcount; i++)
                if (preassignedsats[i] == s)
                    return true;
            return false;
        }

        private bool PreAssign(Satellite s)
        {
            if (preassignedcount == LASERS_PER_SAT || s.preassignedcount == LASERS_PER_SAT || IsPreAssigned(s))
                return false;
            Debug.Assert(s.IsPreAssigned(this) == false);

            SimplePreAssign(s);
            s.SimplePreAssign(this);
            return true;
        }

        public void ClearPreAssignedLasers()
        {
            preassignedcount = 0;
        }

        public void PreAssignLasersOrbitalPlane()
        {
            var count = 0;
            var satbase = satid / sats_per_orbit * sats_per_orbit;
            var nextsat = (satid - satbase + 1) % sats_per_orbit + satbase;
            for (var i = 0; i < nearestcount; i++)
                if (nearestsats[i].satid == nextsat)
                {
                    PreAssign(nearestsats[i]);
                    if (count == 2)
                        return;
                    count++;
                }
        }

        // code intended for polar satellites
        public void PreAssignLasers1b()
        {
            var count = 0;
            var satbase = satid / 75 * 75;
            var nextsat = (satid - satbase + 1) % 75 + satbase;
            for (var i = 0; i < nearestcount; i++)
                if (nearestsats[i].satid == nextsat)
                {
                    PreAssign(nearestsats[i]);
                    if (count == 2)
                        return;
                    count++;
                }
        }

        public void PreAssignLasersBetweenPlanes(int plane_shift, int plane_step)
        {
            if (satid < phase1_satcount)
                PreAssignLasersBetweenPlanes1(plane_shift, plane_step);
            else
                PreAssignLasersBetweenPlanes2(plane_shift);
        }

        public void PreAssignLasersBetweenPlanes1(int plane_shift, int plane_step)
        {
            var tmpsatid = satid;
            if (satid >= phase1_satcount) return;
            var count = 0;
            int sideways;

            var modsatid = satid % sats_per_orbit; // id of sat in its plane
            var offset = plane_step * sats_per_orbit + plane_shift; // default offset, ignoring wrapping

            // ensure we connect to the correct plane
            while ((modsatid + offset) / sats_per_orbit < plane_step) offset += sats_per_orbit;
            while ((modsatid + offset) / sats_per_orbit > plane_step) offset -= sats_per_orbit;
            sideways = satid + offset;

            if (sideways >= phase1_satcount)
            {
                // wrap around end of constellation
                var stagger = (int)Mathf.Round((float)(orbital_planes * sat_phase_stagger));
                offset = plane_step * sats_per_orbit + plane_shift + stagger;
                while ((modsatid + offset) / sats_per_orbit < plane_step) offset += sats_per_orbit;
                while ((modsatid + offset) / sats_per_orbit > plane_step) offset -= sats_per_orbit;
                sideways = (satid + offset) % phase1_satcount;
            }

            for (var i = 0; i < nearestcount; i++)
                if (nearestsats[i].satid == sideways)
                {
                    PreAssign(nearestsats[i]);
                    if (count == 2)
                        return;
                    count++;
                }
        }

        // old code for phase 2
        public void PreAssignLasersBetweenPlanes2(int shift)
        {
            var tmpsatid = satid;
            var offset = 0;
            if (satid >= phase1_satcount)
            {
                offset = phase1_satcount;
                tmpsatid -= offset;
            }

            var count = 0;
            int sideways;
            if (tmpsatid % 50 + 50 + shift < 50)
                sideways = tmpsatid + 100 + shift; // don't connect to your own orbital place with negative shifts
            else if (tmpsatid % 50 + 50 + shift >= 100)
                sideways = tmpsatid + shift; // don't connect to two orbital planes over
            else
                sideways = tmpsatid + 50 + shift;

            if (sideways >= 1600)
            {
                var stagger = (int)Mathf.Round((float)(orbital_planes * sat_phase_stagger));
                sideways = (sideways % 1600 + stagger) % 50; // 8 comes from the phase offset per plane 32/4 = 8
            }

            sideways += offset;
            for (var i = 0; i < nearestcount; i++)
                if (nearestsats[i].satid == sideways)
                {
                    PreAssign(nearestsats[i]);
                    if (count == 2)
                        return;
                    count++;
                }
        }


        public void UsePreAssigned()
        {
            for (var i = 0; i < preassignedcount; i++) Assign(preassignedsats[i]);
        }

        private int FindFreeLaser()
        {
            for (var lc = 0; lc < maxlasers; lc++)
                if (!laseron[lc])
                    return lc;
            return -1;
        }

        public void FinalizeLasers(float speed, Material isl_material)
        {
            // Turn off lasers that are no longer assigned to the same target
            for (var lc = 0; lc < maxlasers; lc++)
                if (laseron[lc])
                    if (!IsAssigned(laserdsts[lc]))
                        // laser will need to be reassigned
                        laseron[lc] = false;

            for (var i = 0; i < assignedcount; i++)
            {
                var sat = assignedsats[i];

                if (!WasAssigned(sat))
                {
                    /* destination is a new one - find a laser */
                    var lasernum = FindFreeLaser();
                    laseron[lasernum] = true;
                    lasertimes[lasernum] = Time.time;
                    laserdsts[lasernum] = sat;
                    var ls = (LaserScript)lasers[lasernum].GetComponent(typeof(LaserScript));
                    ls.dest_satid = sat.satid; // remove after (MORGANE)
                    ls.src_satid = satid; // MORGANE: remove after
                    ls.SetMaterial(isl_material);
                }
            }

            var oncount = 0;
            for (var lc = 0; lc < maxlasers; lc++)
                if (laseron[lc])
                {
                    oncount++;
                    var ls = (LaserScript)lasers[lc].GetComponent(typeof(LaserScript));
                    Debug.Assert(this != laserdsts[lc]);
                    Debug.Assert(position() != laserdsts[lc].position());

                    ls.SetPos(position(), laserdsts[lc].position());
                    //ls.SetMaterial (isl_material);
                }
                else
                {
                    var ls = (LaserScript)lasers[lc].GetComponent(typeof(LaserScript));
                    ls.SetPos(position(), position());
                }
        }

        public void ColorLink(Satellite nextsat, Material mat)
        {
            for (var lc = 0; lc < maxlasers; lc++)
                if (laseron[lc])
                    if (laserdsts[lc] ==
                        nextsat) // check all other destinations. If the laser is one of the destinations, then we're good.
                    {
                        var ls = (LaserScript)lasers[lc].GetComponent(typeof(LaserScript));
                        ls.src_satid = satid;
                        ls.dest_satid = nextsat.satid;
                        ls.SetMaterial(mat);
                    }
        }
    }
}