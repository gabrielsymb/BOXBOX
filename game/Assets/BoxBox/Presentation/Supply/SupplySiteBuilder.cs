using UnityEngine;

namespace BoxBox.Presentation.Supply
{
    /// <summary>
    /// Greybox stand-in for a place of business: a coloured parking pad where the vehicle must
    /// stop to trade, and a solid building beside it. Returns the pad area used to detect arrival.
    /// </summary>
    public static class SupplySiteBuilder
    {
        public static Bounds Build(string name, Vector3 padCentre, Vector2 padSize, Vector3 buildingOffset,
            Vector3 buildingSize, Color padColor)
        {
            var root = new GameObject(name).transform;
            root.position = padCentre;

            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = name + "_Parking";
            pad.transform.SetParent(root, false);
            pad.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            pad.transform.localScale = new Vector3(padSize.x, 0.02f, padSize.y);
            pad.GetComponent<Renderer>().material.color = padColor;
            Object.Destroy(pad.GetComponent<Collider>());   // paint only; wheels drive on the ground

            var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = name + "_Building";
            building.transform.SetParent(root, false);
            building.transform.localPosition = buildingOffset + Vector3.up * buildingSize.y / 2f;
            building.transform.localScale = buildingSize;

            return new Bounds(padCentre, new Vector3(padSize.x, 10f, padSize.y));
        }
    }
}
