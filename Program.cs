using System;
using System.Collections.Generic;
using System.Linq;
using static RubiksCube.Program.Color;
using static RubiksCube.Program.Axis;
using static RubiksCube.Program.Side;
using static System.Console;
using System.Text;

namespace RubiksCube
{
    class Program
    {
        static void Rotate<K, V>(Dictionary<K, V> map, params K[] keys)
        {
            var last = map[keys.Last()];
            foreach (var (current, next) in keys.SkipLast(1).Zip(keys.Skip(1)).Reverse())
            {
                map[next] = map[current];
            }
            map[keys.First()] = last;
        }

        public enum Color { White, Red, Yellow, Green, Blue, Orange, Unknown }

        public enum Side { Front, Back, Right, Left, Top, Bottom }

        public enum Axis { X, Y, Z }

        class Face 
        {
            public Color Color; 
            public static implicit operator Face(Color color) => new Face() { Color = color };
        }

        class Block
        {
            public Dictionary<Side, Face> Faces;

            public Block()
            {
                Faces = Enumerable.Range(0, 6).ToDictionary(i => (Side)i, _ => (Face)Unknown);
            }

            public void Rotate(Axis axis)
            {
                var loop = new Dictionary<Axis, Side[]> {
                    { X, new [] { Front, Bottom, Back, Top } }, 
                    { Y, new [] { Front, Right, Back, Left } }, 
                    { Z, new [] { Top, Left, Bottom, Right } }
                };
                Program.Rotate(Faces, loop[axis]);
            }
        }

        class Cube
        {
            Dictionary<(int x, int y, int z), Block> blocks;

            public Cube()
            {
                blocks = (from x in Enumerable.Range(0, 3)
                    from y in Enumerable.Range(0, 3)
                    from z in Enumerable.Range(0, 3)
                    select (x, y, z)).ToDictionary(p => p, _ => new Block());

                new (Side side, Color color)[] { 
                    (Side.Front, Red), 
                    (Side.Back, Orange), 
                    (Side.Left, Green), 
                    (Side.Right, Blue),
                    (Side.Bottom, Yellow), 
                    (Side.Top, White) }.Select( x => 
                        GetSide(x.side)
                        .Cast<Face>()
                        .Select(f => f.Color = x.color)
                        .ToArray())
                    .ToArray();
            }

            public IEnumerable<Face> Top => GetSide(Side.Top);
            public IEnumerable<Face> Bottom => GetSide(Side.Bottom);
            public IEnumerable<Face> Left => GetSide(Side.Left);
            public IEnumerable<Face> Right => GetSide(Side.Right);
            public IEnumerable<Face> Back => GetSide(Side.Back);
            public IEnumerable<Face> Front => GetSide(Side.Front);

            private IEnumerable<Face> GetSide(Side side) =>
                from r in Enumerable.Range(0, 3)
                from c in Enumerable.Range(0, 3)
                select blocks[((side) switch 
                {
                    Side.Front  => (    c, 2 - r,     2), 
                    Side.Back   => (2 - c, 2 - r,     0), 
                    Side.Left   => (    0, 2 - r,     c), 
                    Side.Right  => (    2, 2 - r, 2 - c), 
                    Side.Bottom => (    c,     0, 2 - r), 
                    _           => (    c,     2,     r)       
                })].Faces[side];

            public void Rotate(Axis axis, int plane)
            {
                var loop = new (int x, int y, int z)[] {
                    (0, 0, 0), 
                    (0, 1, 0), (0, 2, 0), 
                    (0, 2, 1), (0, 2, 2), 
                    (0, 1, 2), (0, 0, 2), 
                    (0, 0, 1)
                }
                .Select(p => (x: p.x + plane, y: p.y, z: p.z))
                .Select(p => (axis) switch
                    {
                        X => (p.x, p.y, p.z),
                        Y => (p.z, p.x, p.y),
                        _ => (p.y, p.z, p.x),
                    })
                .ToArray();

                for (int i = 0; i < 2; i++)
                    Program.Rotate(blocks, loop);

                foreach (var p in loop)
                    blocks[p].Rotate(axis);
            }

            private bool SameColor(IEnumerable<Face> faces) => 
                !faces.Where(f => f.Color != faces.First().Color).Any();

            public bool Done => 
                SameColor(Top) 
                && SameColor(Bottom) 
                && SameColor(Left) 
                && SameColor(Right)
                && SameColor(Front)
                && SameColor(Back);

        }

        static void PrintSides(int row, params IEnumerable<Face>[] sides)
        {
            var bg = BackgroundColor;
            foreach (var side in sides)
            {
                foreach (var face in side.Skip(row*3).Take(3))
                {
                    Console.BackgroundColor = (face.Color) switch
                    {
                        Red => ConsoleColor.Red,
                        Blue => ConsoleColor.Blue, 
                        White => ConsoleColor.White,
                        Orange => ConsoleColor.Magenta, 
                        Green => ConsoleColor.Green,
                        Yellow => ConsoleColor.Yellow,
                        _ => bg,
                    };
                    Write(" ");
                }
            }
            BackgroundColor = bg;
        }

        static void Print(Cube cube)
        {
            Clear();
            WriteLine("    123         ");
            Write("    "); 
            PrintSides(row: 0, cube.Top); 
            WriteLine();
            Write("    "); 
            PrintSides(row: 1, cube.Top); 
            WriteLine();
            Write(" 789"); 
            PrintSides(row: 2, cube.Top); 
            WriteLine();
            for (int i = 0; i < 3; i++)
            {
                Write((6 - i));
                PrintSides(row: i, cube.Left, cube.Front, cube.Right, cube.Back);
                WriteLine();
            }
            for (int i = 0; i < 3; i++)
            {
                Write("    ");
                PrintSides(row: i, cube.Bottom); 
                WriteLine();
            }
        }


        static void Main(string[] args)
        {
            Console.CursorVisible = false;

            Cube cube = new Cube();
            
            Random rng = new Random();

            for (int i = 0; i < 100; ++i)
            {
                cube.Rotate((Axis)rng.Next(3), rng.Next(3));
            }

            Print(cube);

            var rotations = new Dictionary<ConsoleKey, (Axis axis, int plane)>() 
            {
                { ConsoleKey.D1, (X, 0) },
                { ConsoleKey.D2, (X, 1) },
                { ConsoleKey.D3, (X, 2) },
                { ConsoleKey.D4, (Y, 0) },
                { ConsoleKey.D5, (Y, 1) },
                { ConsoleKey.D6, (Y, 2) },
                { ConsoleKey.D7, (Z, 0) },
                { ConsoleKey.D8, (Z, 1) },
                { ConsoleKey.D9, (Z, 2) }
            };

            while(!cube.Done)
            {
                var key = ReadKey().Key;
                if (rotations.ContainsKey(key))
                {
                    var rotation = rotations[key];
                    cube.Rotate(rotation.axis, rotation.plane);
                } 
                Print(cube);
            }
        }
    }
}
