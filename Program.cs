namespace Chip8;

using Raylib_cs;

class Program
{
    // Mapping QWERTY to the original Chip8 keys.
    // https://deepwiki.com/tavito/chip8/5.2-keyboard-input#key-mapping
    static readonly Dictionary<KeyboardKey, byte> Keyboard = new()
    {
        [KeyboardKey.One] = 0x1,
        [KeyboardKey.Two] = 0x2,
        [KeyboardKey.Three] = 0x3,
        [KeyboardKey.Four] = 0xC,
        [KeyboardKey.Q] = 0x4,
        [KeyboardKey.W] = 0x5,
        [KeyboardKey.E] = 0x6,
        [KeyboardKey.R] = 0xD,
        [KeyboardKey.A] = 0x7,
        [KeyboardKey.S] = 0x8,
        [KeyboardKey.D] = 0x9,
        [KeyboardKey.F] = 0xE,
        [KeyboardKey.Z] = 0xA,
        [KeyboardKey.X] = 0x0,
        [KeyboardKey.C] = 0xB,
        [KeyboardKey.V] = 0xF,
    };

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            throw new Exception("A Chip8 ROM must be provided.");
        }

        var program = File.ReadAllBytes(args[0]);

        var chip8 = new Chip8Emulator(program);
        var chip8dt = 0.0;

        Raylib.InitWindow(640, 320, "Chip8 Emulator");
        Raylib.SetTargetFPS(60);

        while (!Raylib.WindowShouldClose())
        {
            // Update emulator's state based on Raylib's delta time.
            float dt = Raylib.GetFrameTime();

            // Handle keyboard input.
            foreach (var (key, mapping) in Keyboard)
            {
                if (Raylib.IsKeyPressed(key))
                {
                    chip8.HandleKeyDown(mapping);
                }

                if (Raylib.IsKeyReleased(key))
                {
                    chip8.HandleKeyUp(mapping);
                }
            }

            chip8.PerformCpuCycle(dt);
            chip8dt += dt;

            if (chip8dt > 1.0 / 60.0)
            {
                chip8.DecrementTimers();
                chip8dt -= 1.0 / 60.0;
            }

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);

            for (int x = 0; x < Display.DisplayWidth; x++)
            {
                for (int y = 0; y < Display.DisplayHeight; y++)
                {
                    // Calculate the pixel's position in the display buffer and 
                    // determine whether it should be drawn.
                    int index = x + Display.DisplayWidth * y;

                    if (chip8.Display.Buffer[index] == 1)
                    {
                        Raylib.DrawRectangle(x * 10, y * 10, 10, 10, Color.White);
                    }
                }
            }

            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
