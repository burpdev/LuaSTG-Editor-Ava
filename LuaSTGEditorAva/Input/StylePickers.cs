using Avalonia.Media;using Avalonia.Layout;using Avalonia.Interactivity;using Avalonia.Input;using Avalonia.Data;using Avalonia.Controls.Templates;using Avalonia.Controls.Primitives;using Avalonia.Controls;using Avalonia;
using System.Collections.Generic;
namespace LuaSTGEditorAva.Input
{
    public sealed class BulletPickerWindow : IconPickerWindow
    {
        private static readonly (string, string)[] Options =
        {
            ("arrow_big", "/LuaSTGNode.Legacy;component/images/bullet/scale.png"),
            ("arrow_mid", "/LuaSTGNode.Legacy;component/images/bullet/arrow.png"),
            ("arrow_small", "/LuaSTGNode.Legacy;component/images/bullet/chain.png"),
            ("gun_bullet", "/LuaSTGNode.Legacy;component/images/bullet/bullet.png"),
            ("butterfly", "/LuaSTGNode.Legacy;component/images/bullet/butterfly.png"),
            ("square", "/LuaSTGNode.Legacy;component/images/bullet/ofuda.png"),
            ("ball_small", "/LuaSTGNode.Legacy;component/images/bullet/point.png"),
            ("ball_mid", "/LuaSTGNode.Legacy;component/images/bullet/smallball.png"),
            ("ball_mid_b", "/LuaSTGNode.Legacy;component/images/bullet/smallball_b.png"),
            ("ball_mid_c", "/LuaSTGNode.Legacy;component/images/bullet/circle.png"),
            ("ball_mid_d", "/LuaSTGNode.Legacy;component/images/bullet/smallball_d.png"),
            ("ball_big", "/LuaSTGNode.Legacy;component/images/bullet/middleball.png"),
            ("ball_huge", "/LuaSTGNode.Legacy;component/images/bullet/bigball.png"),
            ("ball_light", "/LuaSTGNode.Legacy;component/images/bullet/lightball.png"),
            ("star_small", "/LuaSTGNode.Legacy;component/images/bullet/smallstar.png"),
            ("star_big", "/LuaSTGNode.Legacy;component/images/bullet/bigstar.png"),
            ("grain_a", "/LuaSTGNode.Legacy;component/images/bullet/grain.png"),
            ("grain_b", "/LuaSTGNode.Legacy;component/images/bullet/needle.png"),
            ("grain_c", "/LuaSTGNode.Legacy;component/images/bullet/blackgrain.png"),
            ("kite", "/LuaSTGNode.Legacy;component/images/bullet/drip.png"),
            ("knife", "/LuaSTGNode.Legacy;component/images/bullet/sword.png"),
            ("knife_b", "/LuaSTGNode.Legacy;component/images/bullet/knife.png"),
            ("water_drop", "/LuaSTGNode.Legacy;component/images/bullet/fire.png"),
            ("mildew", "/LuaSTGNode.Legacy;component/images/bullet/mildew.png"),
            ("ellipse", "/LuaSTGNode.Legacy;component/images/bullet/ellipse.png"),
            ("heart", "/LuaSTGNode.Legacy;component/images/bullet/heart.png"),
            ("money", "/LuaSTGNode.Legacy;component/images/bullet/money.png"),
            ("music", "/LuaSTGNode.Legacy;component/images/bullet/music.png"),
            ("silence", "/LuaSTGNode.Legacy;component/images/bullet/silence.png"),
            ("water_drop_dark", "/LuaSTGNode.Legacy;component/images/bullet/fire_dark.png"),
            ("ball_huge_dark", "/LuaSTGNode.Legacy;component/images/bullet/bigball_dark.png"),
            ("ball_light_dark", "/LuaSTGNode.Legacy;component/images/bullet/lightball_dark.png"),
            ("star_big_b", "/LuaSTGNode.Legacy;component/images/bullet/bigstar_b.png"),
        };

        public BulletPickerWindow(string s)
            : base("Bullet style", Options)
        {
            Result = s;
        }
    }

    public sealed class LaserPickerWindow : IconPickerWindow
    {
        private static readonly (string, string)[] Options =
        {
            ("1", "/LuaSTGNode.Legacy;component/images/laser/laser1.png"),
            ("2", "/LuaSTGNode.Legacy;component/images/laser/laser2.png"),
            ("3", "/LuaSTGNode.Legacy;component/images/laser/laser3.png"),
            ("4", "/LuaSTGNode.Legacy;component/images/laser/laser4.png"),
        };

        public LaserPickerWindow(string s)
            : base("Laser style", Options, columns: 4)
        {
            Result = s;
        }
    }

    public sealed class BubblePickerWindow : IconPickerWindow
    {
        private static readonly (string, string)[] Options =
        {
            ("1", "bubble_1.png"),
            ("2", "bubble_2.png"),
            ("3", "bubble_3.png"),
            ("4", "bubble_4.png"),
        };

        public BubblePickerWindow(string s)
            : base("Bubble style", Options, columns: 4)
        {
            Result = s;
        }
    }

    public sealed class EnemyPickerWindow : IconPickerWindow
    {
        private static readonly (string, string)[] Options = BuildOptions();

        private static (string, string)[] BuildOptions()
        {
            var tags = new[] { "1", "2", "3", "4", "5", "6", "7", "8", "10", "11", "12", "13",
                "15", "16", "17", "18", "19", "20", "20", "22", "23", "24", "25", "26", "27",
                "28", "29", "30", "31", "32", "33", "34", "9", "14" };
            var images = new[] { "enemy1", "enemy2", "enemy3", "enemy4", "enemy5", "enemy6",
                "enemy7", "enemy8", "enemy10", "enemy11", "enemy12", "enemy13", "enemy15",
                "enemy16", "enemy17", "enemy18", "enemy19", "enemy20", "enemy21", "enemy22",
                "enemy23", "enemy24", "enemy25", "enemy26", "enemy27", "enemy28", "enemy29",
                "enemy30", "enemy31", "enemy32", "enemy33", "enemy34", "enemy9", "enemy14" };
            var list = new List<(string, string)>();
            for (int i = 0; i < tags.Length; i++)
                list.Add((tags[i], $"/LuaSTGNode.Legacy;component/images/enemy/{images[i]}.png"));
            return list.ToArray();
        }

        public EnemyPickerWindow(string s)
            : base("Enemy style", Options)
        {
            Result = s;
        }
    }

    public sealed class ColorPickerWindow : IconPickerWindow
    {
        private static readonly string[] Names =
        {
            "COLOR_RED", "COLOR_DEEP_RED", "COLOR_PURPLE", "COLOR_DEEP_PURPLE",
            "COLOR_BLUE", "COLOR_DEEP_BLUE", "COLOR_ROYAL_BLUE", "COLOR_CYAN",
            "COLOR_DEEP_GREEN", "COLOR_GREEN", "COLOR_CHARTREUSE", "COLOR_YELLOW",
            "COLOR_GOLDEN_YELLOW", "COLOR_ORANGE", "COLOR_DEEP_GRAY", "COLOR_GRAY",
        };

        private static IEnumerable<(string, string)> BuildOptions()
        {
            foreach (string n in Names)
                yield return (n, $"/LuaSTGNode.Legacy;component/images/color/{n}.png");
        }

        public ColorPickerWindow(string s)
            : base("Color", BuildOptions(), columns: 8)
        {
            Result = s;
        }
    }
}
