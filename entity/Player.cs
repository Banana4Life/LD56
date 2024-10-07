using Godot;
using LD56;

public partial class Player : CharacterBody2D
{
    public const float SPEED = 300.0f;

    [Export] public string DisplayName;
    [Export] public int PlayerSize;
    public Vector2 targetScale;

    public double eatenCd;


    public override void _Ready()
    {
        // var syncher = GetNode<MultiplayerSynchronizer>("PlayerSync");
        // syncher.SetVisibilityFor(0, false);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsMultiplayerAuthority())
        {
            Velocity = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down") * SPEED;
            MoveAndSlide();
            for (int i = 0; i < GetSlideCollisionCount(); i++)
            {
                var collision = GetSlideCollision(i);
                // GD.Print($"{DisplayName} collided {collision.GetCollider()}");

                if (collision.GetCollider() is RigidBody2D rb)
                {
                    rb.ApplyCentralImpulse(-collision.GetNormal() * 5);
                }
                if (collision.GetCollider() is Particle pa)
                {
                    if (pa.size < PlayerSize)
                    {
                        if (pa.eatenCd < 0)
                        {
                            pa.eatenCd = 0.1;
                            var massEaten = (int) (Mathf.Max(5, pa.size * delta * 25));
                            GrowPlayer(Mathf.Max(1, massEaten / 2));
                            RpcId(1, MethodName.EatParticle, pa.Name, massEaten);    
                        }
                        
                    }
                }

                if (collision.GetCollider() is Player pl)
                {
                    if (PlayerSize > pl.PlayerSize)
                    {
                        if (pl.eatenCd < 0)
                        {
                            var massEaten = (int) (Mathf.Max(5, pl.PlayerSize * delta * 25));
                            GrowPlayer(Mathf.Max(1, massEaten / 4));
                            GD.Print($"{Multiplayer.GetUniqueId()} : {DisplayName} eats {massEaten} of {pl.DisplayName}");
                            RpcId(int.Parse(pl.Name), MethodName.EatPlayer, pl.Name, massEaten);
                            pl.eatenCd = 0.1;
                        }
                    }
                }
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void EatParticle(string name, int mass)
    {
        var particle = GetParent().GetNode<Particle>(name);
        particle.size -= mass;
        
        var world = GetParent<World>();
        world.totalMass -= mass;
    }

    
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void EatPlayer(string name, int mass)
    {
        var player = GetParent().GetNode<Player>(name);
        player.GrowPlayer(-mass);
    }

    public override void _Process(double delta)
    {
        eatenCd -= delta;
        GetNode<Label>("Label").Text = DisplayName;
        var scaled = GetNode<Node2D>("scaled");
        var collider = GetNode<CollisionShape2D>("PhysicsCollisionShape");
        scaled.Scale = scaled.Scale.Lerp(targetScale, (float) delta);
        collider.Scale = scaled.Scale;
        
        var scale = Mathf.Sqrt(PlayerSize / Mathf.Pi) * 2 / 10f;
        targetScale = new Vector2(scale, scale);
    }

    public void _enter_tree()
    {
        SetMultiplayerAuthority(int.Parse(Name));
    }

    public void GrowPlayer(int mass = 200)
    {
        PlayerSize += mass;
        if (mass > 0)
        {
            GD.Print($"{DisplayName}({Name}) grows to {PlayerSize} (+{mass})");
        }
        else
        {
            GD.Print($"{DisplayName}({Name}) shrinks to {PlayerSize} (-{mass})");
        }
        if (PlayerSize < 0)
        {
            PlayerDied();
        }
    }

    public void PlayerDied()
    {
        GetParent<World>().authorityPlayer = null; 
        Global.Instance.PlayerManager.RemovePlayer(int.Parse(Name)); // TODO on server?
        QueueFree(); 
        // TODO you died respawn screen?
        // TODO particles
    }
}