using UnityEngine;

namespace Orbits
{
    public class LaserScript : MonoBehaviour
    {
        public LineRenderer line;

        public int src_satid; 
        public int dest_satid;

        // Use this for initialization
        private void Start()
        {
            line = GetComponent<LineRenderer>();
        }

        // Update is called once per frame
        private void Update()
        {
        }

        public void SetPos(Vector3 pos1, Vector3 pos2)
        {
            line.SetPosition(0, pos1);
            line.SetPosition(1, pos2);
        }

        public void SetMaterial(Material mat)
        {
            line.material = mat;
        }

        public void ChangeMaterial(Material newMat)
        {
            line.material = newMat;
        }
    }
}