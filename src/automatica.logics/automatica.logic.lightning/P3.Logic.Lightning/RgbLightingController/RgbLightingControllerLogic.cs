using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Automatica.Core.Base.IO;
using Automatica.Core.EF.Models;
using Automatica.Core.Logic;
using P3.Logic.Lightning.Common;

namespace P3.Logic.Lightning.RgbLightingController
{
    /// <summary>
    /// A colour in percent per channel (0..100), the Loxone RGB value is R + G·1000 + B·1000000
    /// </summary>
    public readonly record struct RgbPercent(int R, int G, int B)
    {
        public static RgbPercent Black => new(0, 0, 0);
        public static RgbPercent White => new(100, 100, 100);

        public int Loxone => R + G * 1000 + B * 1000000;

        public string Hex => LightValue.Format(R * 255 / 100.0, G * 255 / 100.0, B * 255 / 100.0);

        private static int Clamp(double v) => (int)Math.Round(Math.Clamp(v, 0, 100));

        public static RgbPercent FromLoxone(long value)
        {
            if (value < 0)
            {
                return Black;
            }
            return new RgbPercent(Clamp(value % 1000), Clamp(value / 1000 % 1000), Clamp(value / 1000000 % 1000));
        }

        /// <summary>
        /// "#RRGGBB", a Loxone value (BBBGGGRRR) or "r,g,b" in percent; null when it is none of them
        /// </summary>
        public static RgbPercent? Parse(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case string s:
                    s = s.Trim();
                    if (s.StartsWith('#'))
                    {
                        return LightValue.TryParseColor(s, out var r, out var g, out var b)
                            ? new RgbPercent(Clamp(r / 2.55), Clamp(g / 2.55), Clamp(b / 2.55))
                            : null;
                    }
                    var parts = s.Split(',');
                    if (parts.Length == 3)
                    {
                        var p = parts.Select(x => double.TryParse(x.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : double.NaN).ToArray();
                        return p.Any(double.IsNaN) ? null : new RgbPercent(Clamp(p[0]), Clamp(p[1]), Clamp(p[2]));
                    }
                    return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? FromLoxone((long)n) : null;
                default:
                    var number = LightingValues.ToDouble(value, double.NaN);
                    return double.IsNaN(number) ? null : FromLoxone((long)number);
            }
        }
    }

    /// <summary>
    /// Scenes 1..n come from the parameter (";"-separated colours), 0 is off, 99 all on (white), -1 a colour set directly.
    /// + / - cycle 1..n (from off + starts with the first, - with the last); Dis = 1 ignores every input.
    /// </summary>
    public class RgbLightingControllerLogic : ClockedLogic
    {
        public const int SceneOff = 0;
        public const int SceneAllOn = 99;
        public const int SceneDirect = -1;

        private readonly Dictionary<Guid, bool> _edges = new();
        private List<RgbPercent> _scenes = new();
        private RgbPercent _color = RgbPercent.Black;
        private int _scene = SceneOff;
        private bool _disabled;

        public RgbLightingControllerLogic(ILogicContext context) : base(context)
        {
            _scenes = ParseScenes(RgbLightingControllerLogicFactory.DefaultScenes);
        }

        internal IReadOnlyList<RgbPercent> SceneList => _scenes;

        internal static List<RgbPercent> ParseScenes(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<RgbPercent>();
            }
            return text.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => RgbPercent.Parse(s))
                .Where(c => c.HasValue)
                .Select(c => c.Value)
                .ToList();
        }

        protected override void ApplyParameter(Guid template, object value)
        {
            if (template == RgbLightingControllerLogicFactory.Scenes)
            {
                _scenes = ParseScenes(value as string);
            }
        }

        private bool Rising(Guid template, object value)
        {
            var state = LightingValues.IsTruthy(value);
            _edges.TryGetValue(template, out var previous);
            _edges[template] = state;
            return state && !previous;
        }

        protected override void OnInput(Guid template, object value, IDispatchable source, DateTime now)
        {
            if (template == RgbLightingControllerLogicFactory.Disable)
            {
                _disabled = LightingValues.IsTruthy(value);
                return;
            }

            // keep the edges up to date while disabled, a held button must not fire when Dis goes back to 0
            var rising = Rising(template, value);
            if (_disabled)
            {
                return;
            }

            if (template == RgbLightingControllerLogicFactory.Color)
            {
                var color = RgbPercent.Parse(value);
                if (color.HasValue)
                {
                    Set(color.Value, color.Value == RgbPercent.Black ? SceneOff : SceneDirect);
                }
            }
            else if (template == RgbLightingControllerLogicFactory.Plus && rising)
            {
                Step(+1);
            }
            else if (template == RgbLightingControllerLogicFactory.Minus && rising)
            {
                Step(-1);
            }
            else if (template == RgbLightingControllerLogicFactory.Scene)
            {
                SelectScene(LightingValues.ToInt(value, -1));
            }
            else if (template == RgbLightingControllerLogicFactory.Reset && rising)
            {
                Set(RgbPercent.Black, SceneOff);
            }
            else if (template == RgbLightingControllerLogicFactory.AllOn && rising)
            {
                Set(RgbPercent.White, SceneAllOn);
            }
        }

        private void SelectScene(int scene)
        {
            if (scene == SceneOff)
            {
                Set(RgbPercent.Black, SceneOff);
            }
            else if (scene == SceneAllOn)
            {
                Set(RgbPercent.White, SceneAllOn);
            }
            else if (scene >= 1 && scene <= _scenes.Count)
            {
                Set(_scenes[scene - 1], scene);
            }
        }

        private void Step(int direction)
        {
            if (_scenes.Count == 0)
            {
                return;
            }
            int next;
            if (_scene >= 1 && _scene <= _scenes.Count)
            {
                next = ((_scene - 1 + direction) % _scenes.Count + _scenes.Count) % _scenes.Count + 1;
            }
            else
            {
                next = direction > 0 ? 1 : _scenes.Count;
            }
            SelectScene(next);
        }

        private void Set(RgbPercent color, int scene)
        {
            _color = color;
            _scene = scene;
            Output(Interface(RgbLightingControllerLogicFactory.Red), color.R);
            Output(Interface(RgbLightingControllerLogicFactory.Green), color.G);
            Output(Interface(RgbLightingControllerLogicFactory.Blue), color.B);
            Output(Interface(RgbLightingControllerLogicFactory.Combined), color.Loxone);
            Output(Interface(RgbLightingControllerLogicFactory.Hex), color.Hex);
            Output(Interface(RgbLightingControllerLogicFactory.ActiveScene), scene);
        }

        protected override DateTime? OnTick(DateTime now) => null;
    }
}
