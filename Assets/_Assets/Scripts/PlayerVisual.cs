using UnityEngine;

public class PlayerVisual : MonoBehaviour {
    [SerializeField] private MeshRenderer headMeshRenderer;
    [SerializeField] private MeshRenderer bodyMeshRenderer;

    private Material _material;

    void Awake() {
        // Clone the material for each player so they are affecting only thier
        // own copy
        _material = new Material(headMeshRenderer.material);
        headMeshRenderer.material = _material;
        bodyMeshRenderer.material = _material;
    }

    public void SetPlayerColor(Color color) {
        _material.color = color;
    }
}
