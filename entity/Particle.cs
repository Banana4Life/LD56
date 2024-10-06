using Godot;

public partial class Particle : RigidBody2D
{
    private bool validSpawn = false;
    private double aliveTime = 0;
    [Export] public int size;
    [Export] public Color Color;
    [Export] public float seed;
    [Export] public float mag;
    [Export] public float freq;
    

    public override void _Ready()
    {
        var syncher = GetNode<MultiplayerSynchronizer>("ParticleSync");
        syncher.SetVisibilityFor(0, false);
        
        var sprite = GetNode<Sprite2D>("scaled/Sprite2D");
        sprite.Visible = false;
        var shaderMat = (sprite.Material as ShaderMaterial);
        shaderMat.SetShaderParameter("bodyColor", Color);
        shaderMat.SetShaderParameter("cellColor", Colors.Black);
        shaderMat.SetShaderParameter("uSeed", seed);
        shaderMat.SetShaderParameter("uMagnitude", mag);
        shaderMat.SetShaderParameter("uFrequency", freq);
    }

    public void RandomInit(RandomNumberGenerator random)
    {
        size = random.RandiRange(10, 500);
        
        var scale = Mathf.Sqrt(size / Mathf.Pi) * 2 / 10f;
        GetNode<Node2D>("scaled").Scale = new Vector2(scale, scale);
        GetNode<CollisionShape2D>("PhysicsCollisionShape").Scale = new Vector2(scale, scale);
        
        var color = Color.FromHsv(random.RandfRange(0, 1f), 1f, 1f, random.RandfRange(0.2f, 0.4f));
        Color = color;
        seed = random.RandfRange(0f, 10000f);
        mag = random.RandfRange(0.01f, 0.19f);
        freq = random.RandfRange(0.5f, 5.5f);
    }

    public void RemoveFromGame()
    {
        if (!IsQueuedForDeletion())
        {
            var world = (World)GetParent();
            world.totalMass -= size;
        }

        QueueFree();
    }

    public void _on_area_2d_area_entered(Area2D area)
    {
        // GD.Print($"area entered {area} {this}");
        var otherParent = area.GetParent(); // TODO other things that collide?
        
        if (otherParent is Particle otherParticle) // Spawning particle
        {
            if (validSpawn)
            {
                if (!otherParticle.validSpawn)
                {
                    otherParticle.RemoveFromGame();
                }
                else
                {
                    // TODO how does this happen?
                    GD.Print("Both are valid?");
                }
            }
            else if (otherParticle.validSpawn)
            {
                RemoveFromGame();
            }
            else
            {
                validSpawn = true;
                otherParticle.RemoveFromGame();
            }
        }
        if (area.Name == "PlayerArea")
        {
            if (validSpawn)
            {
                area.GetParent().GetParent<Player>().GrowPlayer(size);
            }
            RemoveFromGame();
        }
        if (otherParent is Player player)
        {
            if (validSpawn)
            {
                player.GrowPlayer(size);
            }
            RemoveFromGame();
        }
        
    }

    public override void _IntegrateForces(PhysicsDirectBodyState2D state)
    {
        var random = new RandomNumberGenerator();
        state.LinearVelocity = state.LinearVelocity.Lerp(new Vector2(random.Randf() - 0.5f, random.Randf() - 0.5f) * 50f, 0.1f);
    }

    public override void _Process(double delta)
    {
        aliveTime += delta;
        if (aliveTime >= 0.5d)
        {
            validSpawn = true;
            // GD.Print("Spawned particle " + size);
            
            var syncher = GetNode<MultiplayerSynchronizer>("ParticleSync");
            syncher.SetVisibilityFor(0, true);
        }

        GetNode<Sprite2D>("scaled/Sprite2D").Visible = validSpawn;
    }
    
}