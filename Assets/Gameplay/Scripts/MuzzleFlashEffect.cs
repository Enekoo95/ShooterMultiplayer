using UnityEngine;

/// <summary>
/// Generador de efecto de muzzle flash dinámicamente.
/// Se instancia y se auto-destruye después de su duración.
/// </summary>
public class MuzzleFlashEffect : MonoBehaviour
{
    public ParticleSystem particles;
    public Light flashLight;
    private float duration = 0.1f;
    private float timer = 0f;

    public static GameObject CreateMuzzleFlash(Vector3 position, Quaternion rotation, float duration = 0.1f)
    {
        // Crear objeto contenedor
        GameObject flashObject = new GameObject("MuzzleFlash");
        flashObject.transform.position = position;
        flashObject.transform.rotation = rotation;

        // Agregar componente de efecto
        MuzzleFlashEffect effect = flashObject.AddComponent<MuzzleFlashEffect>();
        effect.duration = duration;

        // Crear sistema de partículas
        GameObject particleObj = new GameObject("Particles");
        particleObj.transform.parent = flashObject.transform;
        particleObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
        effect.particles = ps;

        // Configurar sistema de partículas para efecto de flash
        ConfigureParticleSystem(ps);

        // Crear luz de destello
        GameObject lightObj = new GameObject("Light");
        lightObj.transform.parent = flashObject.transform;
        lightObj.transform.localPosition = Vector3.zero;

        Light light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.intensity = 2f;
        light.range = 10f;
        light.color = Color.yellow;
        effect.flashLight = light;

        return flashObject;
    }

    private static void ConfigureParticleSystem(ParticleSystem ps)
    {
        // Configurar módulo de emisión
        var emission = ps.emission;
        emission.rateOverTime = 100f;
        emission.burstCount = 1;

        // Configurar módulo de forma
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 25f;

        // Configurar módulo de velocidad inicial
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-5f, 5f);
        velocity.y = new ParticleSystem.MinMaxCurve(-5f, 5f);
        velocity.z = new ParticleSystem.MinMaxCurve(5f, 15f);

        // Configurar módulo de tamaño
        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(0.5f, 2f);

        // Configurar módulo de color/alpha
        var colorOverLife = ps.colorOverLifetime;
        colorOverLife.enabled = true;
        
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.yellow, 0.0f),
                new GradientColorKey(Color.white, 0.5f),
                new GradientColorKey(Color.red, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0.0f),
                new GradientAlphaKey(1f, 0.5f),
                new GradientAlphaKey(0f, 1.0f)
            }
        );
        colorOverLife.color = new ParticleSystem.MinMaxGradient(gradient);

        // Configurar duración
        var main = ps.main;
        main.duration = 0.2f;
        main.loop = false;

        // Configurar renderer
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer == null)
        {
            renderer = ps.gameObject.AddComponent<ParticleSystemRenderer>();
        }

        // Usar material simple para las partículas
        Material particleMaterial = new Material(Shader.Find("Standard"));
        particleMaterial.color = Color.yellow;
        renderer.material = particleMaterial;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        // Desvanecerse la luz
        if (flashLight != null)
        {
            flashLight.intensity = Mathf.Lerp(2f, 0f, timer / duration);
        }

        // Destruir después de la duración
        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}
