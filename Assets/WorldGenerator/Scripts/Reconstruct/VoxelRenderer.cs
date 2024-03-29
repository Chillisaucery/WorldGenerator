

using UnityEngine.Rendering.VirtualTexturing;
using UnityEngine;
using Unity.VisualScripting;

[ExecuteInEditMode]
[RequireComponent(typeof(ParticleSystem))]
public class VoxelRenderer : MonoBehaviour
{
    [SerializeField]
    Reconstruct reconstruct = new Reconstruct();

    ParticleSystem system = null;
    ParticleSystem.Particle[] voxels;
    bool voxelsUpdated = false;

    public float voxelScale = 0.1f;
    public float scale = 1f;

    public bool isMeshActiveLastFrame = false;



    private void OnEnable()
    {
        Generate();
    }

    private void Generate()
    {
        if (system == null) { }
        system = GetComponent<ParticleSystem>();

        Vector3[] positions = reconstruct.Mesh.vertices;

        SetVoxels(positions);
    }

    void Update()
    {
        if (voxelsUpdated)
        {
            system.SetParticles(voxels, voxels.Length);
            voxelsUpdated = false;
        }

        if (reconstruct.gameObject.activeInHierarchy && !isMeshActiveLastFrame)
        {
            Generate();
        }

        isMeshActiveLastFrame = reconstruct.gameObject.activeInHierarchy;
    }

    public void SetVoxels(Vector3[] position)
    {
        voxels = new ParticleSystem.Particle[position.Length];

        for (int i = 0; i < position.Length; i++)
        {
            voxels[i].position = position[i] * scale;
            voxels[i].startColor = Color.red;
            voxels[i].startSize = voxelScale;
        }

        voxelsUpdated = true;
    }
}