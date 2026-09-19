using Avalonia.Media;using Avalonia.Layout;using Avalonia.Interactivity;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using Avalonia.Controls;using Avalonia;using System.Collections.Generic;
using LuaSTGEditorSharp.EditorData.Document;
using LuaSTGEditorSharp.EditorData;
using LuaSTGEditorSharp.Windows.Input;
using LuaSTGEditorSharp.Windows;
using System;
namespace LuaSTGEditorAva.Input
{
    public sealed class AvaloniaInputRegistry : IInputWindowSelectorRegister
    {
        public void RegisterComboBoxText(Dictionary<string, string[]> target)
        {
            target.Add("bool", new string[] { "true", "false" });
            target.Add("bubble_style", new string[] { "1", "2", "3", "4" });
            target.Add("sineinterpolation", new string[] { "SINE_ACCEL", "SINE_DECEL", "SINE_ACC_DEC" });
            target.Add("target", new string[] { "self", "last", "unit", "player", "_boss" });
            target.Add("yield", new string[] { "_infinite" });
            target.Add("nullabletarget", new string[] { "", "self", "last", "unit", "player", "_boss" });
            target.Add("blend", new string[] { "\"\"",
                "\"mul+alpha\"", "\"mul+add\"", "\"mul+rev\"", "\"mul+sub\"",
                "\"add+alpha\"", "\"add+add\"", "\"add+rev\"", "\"add+sub\"",
                "\"alpha+bal\"",
                "\"mul+min\"", "\"mul+max\"", "\"mul+mul\"", "\"mul+screen\"",
                "\"add+min\"", "\"add+max\"", "\"add+mul\"", "\"add+screen\"",
                "\"one\"" });
            target.Add("event", new string[] { "frame", "kill", "del", "colli" });
            target.Add("interpolation", new string[] { "MOVE_NORMAL", "MOVE_ACCEL", "MOVE_DECEL", "MOVE_ACC_DEC" });
            target.Add("modification", new string[] { "MODE_SET", "MODE_ADD", "MODE_MUL" });
            target.Add("group", new string[] { "GROUP_GHOST", "GROUP_ENEMY_BULLET", "GROUP_ENEMY", "GROUP_PLAYER_BULLET",
                "GROUP_PLAYER", "GROUP_INDES", "GROUP_ITEM", "GROUP_NONTJT" });
            target.Add("layer", new string[] { "LAYER_BG-5", "LAYER_BG", "LAYER_BG+5", "LAYER_ENEMY-5", "LAYER_ENEMY",
                "LAYER_ENEMY+5", "LAYER_PLAYER_BULLET-5", "LAYER_PLAYER_BULLET", "LAYER_PLAYER_BULLET+5",
                "LAYER_PLAYER-5", "LAYER_PLAYER", "LAYER_PLAYER+5", "LAYER_ITEM-5", "LAYER_ITEM",
                "LAYER_ITEM+5", "LAYER_ENEMY_BULLET-5", "LAYER_ENEMY_BULLET", "LAYER_ENEMY_BULLET+5",
                "LAYER_ENEMY_BULLET_EF-5", "LAYER_ENEMY_BULLET_EF", "LAYER_ENEMY_BULLET_EF+5",
                "LAYER_TOP-5", "LAYER_TOP", "LAYER_TOP+5" });
            target.Add("stageGroup", new string[] { "Easy", "Normal", "Hard", "Lunatic", "Extra" });
            target.Add("objDifficulty", new string[] { "All", "Easy", "Normal", "Hard", "Lunatic" });
            target.Add("difficulty", new string[] { "1", "2", "3", "4", "5" });
            target.Add("SCName", new string[] { "", "「」" });
            target.Add("bulletStyle", new string[] { "arrow_big", "arrow_mid", "arrow_small", "gun_bullet", "butterfly",
                "square", "ball_small", "ball_mid", "ball_mid_b", "ball_mid_c", "ball_mid_d", "ball_big", "ball_huge",
                "ball_light", "star_small", "star_big", "grain_a", "grain_b", "grain_c", "kite",
                "knife", "knife_b", "water_drop", "mildew", "ellipse", "heart", "money", "music",
                "silence", "water_drop_dark", "ball_huge_dark", "ball_light_dark", "star_big_b" });
            target.Add("laserStyle", new string[] { "1", "2", "3", "4" });
            target.Add("alignInput", new string[] { "0", "1", "2", "4", "5", "6", "8", "9", "10" });
            target.Add("color", new string[] { "COLOR_RED", "COLOR_DEEP_RED", "COLOR_PURPLE", "COLOR_DEEP_PURPLE",
                "COLOR_BLUE", "COLOR_DEEP_BLUE", "COLOR_ROYAL_BLUE", "COLOR_CYAN", "COLOR_DEEP_GREEN",
                "COLOR_GREEN", "COLOR_CHARTREUSE", "COLOR_YELLOW", "COLOR_GOLDEN_YELLOW", "COLOR_ORANGE",
                "COLOR_DEEP_GRAY", "COLOR_GRAY" });
            target.Add("nullableColor", new string[] { "", "COLOR_RED", "COLOR_DEEP_RED", "COLOR_PURPLE", "COLOR_DEEP_PURPLE",
                "COLOR_BLUE", "COLOR_DEEP_BLUE", "COLOR_ROYAL_BLUE", "COLOR_CYAN", "COLOR_DEEP_GREEN",
                "COLOR_GREEN", "COLOR_CHARTREUSE", "COLOR_YELLOW", "COLOR_GOLDEN_YELLOW", "COLOR_ORANGE",
                "COLOR_DEEP_GRAY", "COLOR_GRAY" });
            target.Add("objimage", new string[] { "\"img_void\"", "\"white\"", "\"leaf\"" });
            target.Add("image", new string[] { "\"img_void\"", "\"white\"", "\"leaf\"" });
            target.Add("BG", new string[] { "temple_background", "magic_forest_background", "bamboo_background",
                "bamboo2_background", "cube_background", "gensokyosora_background", "hongmoguanB_background",
                "icepool_background", "lake_background", "le03_5_background", "magic_forest_fast_background",
                "river_background", "starlight_background", "temple2_background", "woods_background",
                "world_background" });
            target.Add("prop", new string[] { "x", "y", "rot", "omiga", "timer", "vx", "vy", "ax", "ay", "layer", "group",
                "hide", "bound", "navi", "colli", "status", "hscale", "vscale", "a", "b", "rect", "img",
                "pause", "rmove", "nopause", "_angle", "_speed" });
            target.Add("valprop", new string[] { "x", "y", "rot", "omiga", "timer", "vx", "vy", "ax", "ay", "layer", "group",
                "hscale", "vscale", "a", "b", "_angle", "_speed" });
            target.Add("se", new string[] { "alert", "astralup", "bonus", "bonus2", "boon00", "boon01", "cancel00",
                "cardget", "cat00", "cat01", "ch00", "ch01", "ch02", "don00", "damage00", "damage01",
                "enep00", "enep01", "enep02", "extend", "fault", "graze", "gun00", "hint00", "invalid",
                "item00", "kira00", "kira01", "kira02", "lazer00", "lazer01", "lazer02", "msl", "msl2",
                "nep00", "ok00", "option", "pause", "pldead00", "plst00", "power0", "power1", "powerup",
                "select00", "slash", "tan00", "tan01", "tan02", "timeout", "timeout2", "warpl", "warpr",
                "water", "explode", "nice", "nodamage", "power02", "lgods1", "lgods2", "lgods3", "lgods4",
                "lgodsget", "big", "wolf", "noise", "pin00", "powerup1", "old_cat00", "old_enep00",
                "old_extend", "old_gun00", "old_kira00", "old_kira01", "old_lazer01", "old_nep00",
                "old_pldead00", "old_power0", "old_power1", "old_powerup", "hyz_charge00", "hyz_charge01b",
                "hyz_chargeup", "hyz_eterase", "hyz_exattack", "hyz_gosp", "hyz_life1", "hyz_playerdead",
                "hyz_timestop0", "hyz_warning", "bonus3", "border", "changeitem", "down", "extend2",
                "focusfix", "focusfix2", "focusin", "heal", "ice", "ice2", "item01", "ophide", "opshow" });
            target.Add("item", new string[] { "item_power", "item_faith", "item_point", "item_power_large", "item_power_full",
                "item_faith_minor", "item_extend", "item_chip", "item_bomb", "item_bombchip" });
            target.Add("lrstr", new string[] { "\"left\"", "\"right\"" });
            target.Add("directionMode", new string[] { "MOVE_X_TOWARDS_PLAYER", "MOVE_Y_TOWARDS_PLAYER", "MOVE_TOWARDS_PLAYER", "MOVE_RANDOM" });
            target.Add("curve", new string[] { "Bezier", "CR", "Basis2" });
            target.Add("renderOp", new string[] { "Push", "Pop" });
            target.Add("warptarget", new string[] { "Capture", "Apply" });
            target.Add("viewmode", new string[] { "ui", "world", "3d" });
            target.Add("viewpoint", new string[] { "\"eye\"", "\"at\"", "\"3D\"", "\"up\"", "\"z\"", "\"fovy\"", "\"fog\"" });
            target.Add("samplerstate", new string[] { "\"point+wrap\"", "\"point+clamp\"", "\"linear+wrap\"", "\"linear+clamp\"" });
            target.Add("richtexttype", new string[] { "path", "font", "system" });
            target.Add("richtexthalign", new string[] { "\"left\"", "\"center\"", "\"right\"", "nil" });
            target.Add("richtextvalign", new string[] { "\"top\"", "\"middle\"", "\"bottom\"", "nil" });
        }

        private static SelectorWindow Combo(AttrItem src, string value, string comboKey, string title)
        {
            return new SelectorWindow(value, AvaloniaInputSelector.SelectComboBox(comboKey), title);
        }

        public void RegisterInputWindow(Dictionary<string, Func<AttrItem, string, IInputWindow>> target)
        {
            target.Add("bool", (src, tar) => Combo(src, tar, "bool", "Input Bool"));
            target.Add("sineinterpolation", (src, tar) => Combo(src, tar, "sineinterpolation", "Input Sine Interpolation Mode"));
            target.Add("code", (src, tar) => new CodeWindow(tar));
            target.Add("position", (src, tar) => new PositionPickerWindow(tar));
            target.Add("pointSet", (src, tar) => new PointSetPickerWindow(tar));
            target.Add("target", (src, tar) => Combo(src, tar, "target", "Input Target Object"));
            target.Add("imageFile", (src, tar) => new PathWindow(tar, "Image File (*.png;*.jpg;*.bmp;*.qoi)|*.png;*.jpg;*.bmp;*.qoi", src));
            target.Add("particleFile", (src, tar) => new PathWindow(tar, "HGE Particle File (*.psi)|*.psi", src));
            target.Add("fxFile", (src, tar) => new PathWindow(tar, "Shader File (*.fx)|*.fx", src));
            target.Add("fontFile", (src, tar) => new PathWindow(tar, "Font File (*.fnt)|*.fnt", src));
            target.Add("ttfFile", (src, tar) => new PathWindow(tar, "TTF File (*.ttf;*.otf)|*.ttf;*.otf", src));
            target.Add("audioFile", (src, tar) => new PathWindow(tar, "Audio File (*.wav;*.ogg)|*.wav;*.ogg", src));
            target.Add("seFile", (src, tar) => new PathWindow(tar, "Sound Effect File (*.wav;*.ogg)|*.wav;*.ogg", src));
            target.Add("luaFile", (src, tar) => new PathWindow(tar, "Lua File (*.lua)|*.lua", src));
            target.Add("lstgesFile", (src, tar) => new PathWindow(tar, "LuaSTG Sharp File (*.lstges)|*.lstges", src));
            target.Add("modelFile", (src, tar) => new PathWindow(tar, "GLTF Model file (*.gltf;*.glb)|*.gltf;*.glb", src));
            target.Add("plainFile", (src, tar) => new PathWindow(tar, "File (*.*)|*.*", src));
            target.Add("plainMultipleFiles", (src, tar) => new MultiPathWindow(tar, "File (*.*)|*.*", src));
            target.Add("multilineText", (src, tar) => new MultilineWindow(tar));
            target.Add("SCName", (src, tar) => Combo(src, tar, "SCName", "Input Spell Card Name"));
            target.Add("blend", (src, tar) => Combo(src, tar, "blend", "Input Blend Mode Type"));
            target.Add("event", (src, tar) => Combo(src, tar, "event", "Input Event Type"));
            target.Add("interpolation", (src, tar) => Combo(src, tar, "interpolation", "Input Interpolation Type"));
            target.Add("modification", (src, tar) => Combo(src, tar, "modification", "Input Modification Type"));
            target.Add("group", (src, tar) => Combo(src, tar, "group", "Input Group Type"));
            target.Add("layer", (src, tar) => Combo(src, tar, "layer", "Input Layer"));
            target.Add("stageGroup", (src, tar) => Combo(src, tar, "stageGroup", "Input Stage Group"));
            target.Add("objDifficulty", (src, tar) => Combo(src, tar, "objDifficulty", "Input Difficulty"));
            target.Add("difficulty", (src, tar) => Combo(src, tar, "difficulty", "Input Difficulty Value"));
            target.Add("prop", (src, tar) => Combo(src, tar, "prop", "Input Properties"));
            target.Add("directionMode", (src, tar) => Combo(src, tar, "directionMode", "Input Direction Mode"));
            target.Add("curve", (src, tar) => Combo(src, tar, "curve", "Input Curve Type"));
            target.Add("renderOp", (src, tar) => Combo(src, tar, "renderOp", "Input Render Target Operation"));
            target.Add("bulletStyle", (src, tar) => new BulletPickerWindow(tar));
            target.Add("laserStyle", (src, tar) => new LaserPickerWindow(tar));
            target.Add("bubble_style", (src, tar) => new BubblePickerWindow(tar));
            target.Add("enemyStyle", (src, tar) => new EnemyPickerWindow(tar));
            target.Add("userDefinedNode", (src, tar) => new NodeDefPickerWindow(tar, src));
            target.Add("bulletDef", (src, tar) => new DefPickerWindow("Choose Bullet", MetaType.Bullet, src, false).Init(tar));
            target.Add("playerbulletDef", (src, tar) => new DefPickerWindow("Choose Player Bullet", MetaType.PlayerBullet, src, false).Init(tar));
            target.Add("objectDef", (src, tar) => new DefPickerWindow("Choose Object", MetaType.Object, src, false).Init(tar));
            target.Add("laserDef", (src, tar) => new DefPickerWindow("Choose Laser", MetaType.Laser, src, false).Init(tar));
            target.Add("bentLaserDef", (src, tar) => new DefPickerWindow("Choose Bent Laser", MetaType.BentLaser, src, false).Init(tar));
            target.Add("enemyDef", (src, tar) => new DefPickerWindow("Choose Enemy", MetaType.Enemy, src, false).Init(tar));
            target.Add("taskDef", (src, tar) => new DefPickerWindow("Choose Task", MetaType.Task, src, false).Init(tar));
            target.Add("bossDef", (src, tar) => new BossDefPickerWindow(tar, src));
            target.Add("itemDef", (src, tar) => new DefPickerWindow("Choose Item", MetaType.Item, src, false).Init(tar));
            target.Add("objimage", (src, tar) => new ImagePickerWindow(tar, src, allowAnimation: true, allowParticle: true));
            target.Add("image", (src, tar) => new ImagePickerWindow(tar, src, allowAnimation: false, allowParticle: false));
            target.Add("BGM", (src, tar) => new BgmPickerWindow(tar, src));
            target.Add("se", (src, tar) => new SePickerWindow(tar, src));
            target.Add("seWithQuotes", (src, tar) => new SePickerWindow(tar, src));
            target.Add("fx", (src, tar) => new FxPickerWindow(tar, src));
            target.Add("font", (src, tar) => new FontPickerWindow(tar, src));
            target.Add("ttf", (src, tar) => new TtfPickerWindow(tar, src));
            target.Add("model", (src, tar) => new ModelPickerWindow(tar, src));
            target.Add("alignInput", (src, tar) => new AlignPickerWindow(tar));
            target.Add("bulletParam", (src, tar) => new ParamEditorWindow(src, MetaType.Bullet, tar));
            target.Add("playerbulletParam", (src, tar) => new ParamEditorWindow(src, MetaType.PlayerBullet, tar));
            target.Add("objectParam", (src, tar) => new ParamEditorWindow(src, MetaType.Object, tar));
            target.Add("laserParam", (src, tar) => new ParamEditorWindow(src, MetaType.Laser, tar));
            target.Add("bentLaserParam", (src, tar) => new ParamEditorWindow(src, MetaType.BentLaser, tar));
            target.Add("enemyParam", (src, tar) => new ParamEditorWindow(src, MetaType.Enemy, tar));
            target.Add("taskParam", (src, tar) => new ParamEditorWindow(src, MetaType.Task, tar));
            target.Add("itemParam", (src, tar) => new ParamEditorWindow(src, MetaType.Item, tar));
            target.Add("color", (src, tar) => new ColorPickerWindow(tar));
            target.Add("nullableColor", (src, tar) => new ColorPickerWindow(tar));
            target.Add("ARGB", (src, tar) => new ArgbPickerWindow(tar, alpha: true));
            target.Add("RGB", (src, tar) => new ArgbPickerWindow(tar, alpha: false));
            target.Add("vector", (src, tar) => new VectorPickerWindow(tar));
            target.Add("size", (src, tar) => new SizePickerWindow(tar));
            target.Add("bossBG", (src, tar) => new BossBgPickerWindow(tar, src));
            target.Add("scale", AvaloniaInputSelector.NullWindow);
            target.Add("colrow", AvaloniaInputSelector.NullWindow);
            target.Add("velocity", AvaloniaInputSelector.NullWindow);
            target.Add("velocityPos", AvaloniaInputSelector.NullWindow);
            target.Add("rotation", AvaloniaInputSelector.NullWindow);
            target.Add("animinterval", AvaloniaInputSelector.NullWindow);
            target.Add("rect", AvaloniaInputSelector.NullWindow);
            target.Add("rectNonNegative", AvaloniaInputSelector.NullWindow);
            target.Add("omega", AvaloniaInputSelector.NullWindow);
            target.Add("richtexttype", (src, tar) => Combo(src, tar, "richtexttype", "Input RichText Create Type"));
            target.Add("richtexthalign", (src, tar) => Combo(src, tar, "richtexthalign", "Input Horizontal Alignment"));
            target.Add("richtextvalign", (src, tar) => Combo(src, tar, "richtextvalign", "Input Vertical Alignment"));
        }
    }
}
