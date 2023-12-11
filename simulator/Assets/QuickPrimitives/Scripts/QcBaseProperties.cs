using UnityEngine;

namespace QuickPrimitives.Scripts
{
    public class QcBaseProperties
    {
        public Vector3 offset;

        public bool genTextureCoords = true;
        public bool addCollider = true;

        public void CopyFrom(QcBaseProperties source)
        {
            offset = source.offset;
            genTextureCoords = source.genTextureCoords;
            addCollider = source.addCollider;
        }
    }
}