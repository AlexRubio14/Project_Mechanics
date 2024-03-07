using System;
using static UnityEditor.Rendering.ShadowCascadeGUI;

[System.Serializable]
public class AA1_ParticleSystem
{
    Random rnd = new Random();

    [System.Serializable]
    public struct Settings
    {
        public uint objectPoolingParticles;
        public uint poolCount;
        public Vector3C gravity;
        public float bounce;
    }
    public Settings settings;

    [System.Serializable]
    public struct SettingsCascade
    {
        public Vector3C PointA;
        public Vector3C PointB;
        public Vector3C Direction;
        public bool randomDirection;

        public float minImpulse;
        public float maxImpulse;

        public float minParticlesPerSecond;
        public float maxParticlesPerSecond;

        public float minParticlesLifeTime;
        public float maxParticlesLifeTime;
    }
    public SettingsCascade settingsCascade;

    [System.Serializable]
    public struct SettingsCannon
    {
        public Vector3C Start;
        public Vector3C Direction;
        public float openingAngle;

        public float minImpulse;
        public float maxImpulse;

        public float minParticlesPerSecond;
        public float maxParticlesPerSecond;

        public float minParticlesLifeTime;
        public float maxParticlesLifeTime;
    }
    public SettingsCannon settingsCannon;

    [System.Serializable]
    public struct SettingsCollision
    {
        public PlaneC[] planes;
        public SphereC[] spheres;
        public CapsuleC[] capsules;
    }
    public SettingsCollision settingsCollision;

    [System.Serializable]
    public struct SettingsParticle
    {
        public float size;
        public float mass;
    }
    public SettingsParticle settingsParticle;

    [System.Serializable]
    public struct EmissionMode
    {
        public enum Emission { CASCADE, CANNON };
        public Emission mode;
    }
    public EmissionMode emissionMode;


    public struct Particle
    {
        public bool active; 

        public float mass;
        public float size;
        public float lifeTime; 

        public Vector3C force;
        
        public Vector3C position;
        public Vector3C velocity;
        public Vector3C aceleration;

        public void InitParticle(SettingsParticle settings)
        {
            this.active = true;

            this.size = settings.size;
            this.mass = settings.mass;

            this.aceleration = Vector3C.zero;
            this.velocity = Vector3C.zero;
        }

        public void InitParticleInCascade(SettingsCascade settingsCascade)
        {
            Random rnd = new Random();

            this.lifeTime = rnd.Next((int)settingsCascade.minParticlesLifeTime, (int)settingsCascade.maxParticlesLifeTime);

            LineC lineBetweenCascades = LineC.CreateLineFromTwoPoints(settingsCascade.PointA, settingsCascade.PointB);
            // EQ. PARAMETRICA: r(x) = B + x * direction x = 0..1
            this.position = lineBetweenCascades.origin + (lineBetweenCascades.direction * (float)rnd.NextDouble());

            this.force = new Vector3C
                (rnd.Next((int)(settingsCascade.Direction.x * settingsCascade.minImpulse), (int)(settingsCascade.Direction.x * settingsCascade.maxImpulse)),
                rnd.Next((int)(settingsCascade.Direction.y * settingsCascade.minImpulse), (int)(settingsCascade.Direction.y * settingsCascade.maxImpulse)),
                rnd.Next((int)(settingsCascade.Direction.z * settingsCascade.minImpulse), (int)(settingsCascade.Direction.z * settingsCascade.maxImpulse)));
        }

        public void InitParticleInCannon(SettingsCannon settingsCannon)
        {
            Random rnd = new Random();

            this.lifeTime = rnd.Next((int)settingsCannon.minParticlesLifeTime, (int)settingsCannon.maxParticlesLifeTime);

            this.position = settingsCannon.Start;

            this.force = Vector3C.zero;
        }

        public bool CheckLifeTime()
        {
            if(this.lifeTime < 0.0f)
            {
                this.active = false;
                return true;
            }
            return false;

        }

        public void Euler(Settings settings, float dt)
        {
            // Apply forces
            this.force += settings.gravity;

            // Calculate acceleration, velocity and position
            this.aceleration = this.force / this.mass;
            this.velocity = this.velocity + this.aceleration * dt;
            this.position = this.position + this.velocity * dt;
            this.force = Vector3C.zero;

            // Clean forces
            this.force = Vector3C.zero;
        }
    }

    bool start = true;

    float timerOneSecond; 
    float spawnTime, lastTimeSpawned;

    Particle[] particles = null;

    public Particle[] Update(float dt)  
    {
        if(start)
            Start();

        if (lastTimeSpawned > spawnTime)
            SpawnParticle(dt);


        for (int i = 0; i < particles.Length; ++i)
        {
            if (particles[i].active)
            {
                // 1. Comprobar el tiempo de vida
                if (particles[i].CheckLifeTime()) { continue; }
                particles[i].lifeTime -= dt;

                // 2. Calcular euler
                particles[i].Euler(settings, dt);

            }
            else
            {
                // 1. Spawnear fuera de la pantalla
                particles[i].position = Vector3C.one * 100;
            }
        }

        // Si ha pasado 1 segundo canvia el numero de particulas a spawner por segundo
        if (timerOneSecond > 1.0f)
        {
            timerOneSecond -= 1.0f;
            spawnTime = NewSpawnTime(); 
        }

        // Sumamos los timers
        timerOneSecond += dt;
        lastTimeSpawned += dt;

        return particles;
    }

    public void Start()
    {
        start = false;

        settings.poolCount = 0;
        particles = new Particle[settings.objectPoolingParticles];

       spawnTime = NewSpawnTime();
    }

    public float NewSpawnTime()
    {
        int particlesPorSecond = RandomParticlesToSpawn(); 

        if(particlesPorSecond != 0)
            return 1.0f / particlesPorSecond;

        return 1.0f;
    }

    public int RandomParticlesToSpawn()
    {
        switch (emissionMode.mode)
        {
            case EmissionMode.Emission.CASCADE:
                return rnd.Next((int)settingsCascade.minParticlesPerSecond, (int)settingsCascade.maxParticlesPerSecond);
            case EmissionMode.Emission.CANNON:
                return rnd.Next((int)settingsCannon.minParticlesPerSecond, (int)settingsCannon.maxParticlesPerSecond);
            default:
                return 0;
        }
    }

    public void SpawnParticle(float dt)
    {
        // 1. Tiene que spawner alguna particula
        lastTimeSpawned -= spawnTime;
        settings.poolCount++;

        // 2. Comprobar que el pool count no sobrepase la array
        if (settings.poolCount >= settings.objectPoolingParticles) { settings.poolCount = 0; }

        // 3. Comprobar que no esta activa
        if (particles[settings.poolCount].active) { return; }

        // 4. Si no esta activa la seteamos
        particles[settings.poolCount].InitParticle(settingsParticle); 
        switch (emissionMode.mode)
        {
            case EmissionMode.Emission.CASCADE:
                particles[settings.poolCount].InitParticleInCascade(settingsCascade); 
                break;
            case EmissionMode.Emission.CANNON:
                particles[settings.poolCount].InitParticleInCannon(settingsCannon);
                break;
            default:
                break;
        }
    }

    public void Debug()
    {
        foreach (var item in settingsCollision.planes)
        {
            item.Print(Vector3C.red);
        }
        foreach (var item in settingsCollision.capsules)
        {
            item.Print(Vector3C.green);
        }
        foreach (var item in settingsCollision.spheres)
        {
            item.Print(Vector3C.blue);
        }
    }
}
