using Godot;
using System;
using LD56;

record Peer(Guid Id, int PeerId, WebRtcPeerConnection Connection);

public partial class Mainmenu : Control
{
    private const string DEFAULT_C2_BASE_URI = "wss://banana4.life";
    private string c2_base_uri;

    [Export] public PackedScene player_scene;
    [Export] public PackedScene worldScene;
    [Export] public LineEdit playerName;


    public override void _Ready()
    {
        LoadConfig();
        InitMainMenu();
        SetupMultiPlayer();
    }

    private void SetupMultiPlayer()
    {
        Multiplayer.PeerDisconnected += OnPlayerLeave;
    }

    private void InitMainMenu()
    {
        playerName.Text = NameGenerator.RandomName();
    }

    private void LoadConfig()
    {
        var config = new ConfigFile();
        var result = config.Load("user://config.cfg");
        c2_base_uri = DEFAULT_C2_BASE_URI;
        if (result == Error.Ok)
        {
            c2_base_uri = config.GetValue("c2server", "host", Variant.CreateFrom(DEFAULT_C2_BASE_URI)).AsString();
        }
    }


    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void initPlayerOnAuthority(string displayName, long id, Vector2 position, int size)
    {
        var existing = GetNode<Player>(id.ToString());
        existing.GlobalPosition = position;
        existing.PlayerSize = size;
        existing.DisplayName = displayName;
        GD.Print($"{Multiplayer.GetUniqueId()}: Player {displayName}({id}) init size: {existing.PlayerSize} auth {existing.GetMultiplayerAuthority()}");
    }

    private void _on_host_button_pressed()
    {
        Global.Instance.State = new ServerState(Multiplayer, c2_base_uri);
        Global.Instance.PlayerManager.AddPlayer(UI_getPlayerName(), 1);
        GetTree().Root.AddChild(worldScene.Instantiate());
        Visible = false;
    }

    private void _on_join_button_pressed()
    {
        Global.Instance.State = new ClientState(Multiplayer, c2_base_uri, UI_getPlayerName());
        GetTree().Root.AddChild(worldScene.Instantiate());
        Visible = false;
    }

    private string UI_getPlayerName()
    {
        return playerName.Text;
    }

    private void OnPlayerLeave(long playerId)
    {
        // TODO check where the players needs to be removed
        // Server only?
        var found = GetNodeOrNull(playerId.ToString());
        if (found != null)
        {
            RemoveChild(found);
        }
    }

}